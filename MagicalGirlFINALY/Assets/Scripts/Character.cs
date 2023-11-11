using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class Character : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Animator animator;
    [SerializeField] private FrameDataSo frameDataSo;
    [SerializeField] private List<GameObject> hitboxes;
    [SerializeField,ReadOnly] private float gravityMultiplier = 1f;
    
    [Header("Settings")]
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float ledgeGravity = 0.3f;
    [SerializeField] private int ledgeJumpFrames = 30;
    
    [Space]
    [SerializeField] private float groundRange = 0.1f;
    [SerializeField] private float groundCheckHeight = 0.1f;
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private LayerMask platformLayerDrop;
    [Space]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private int maxAirJumps = 2;
    [SerializeField] private bool ledgeJumpIsAirJump = true;
    
    [SerializeField,ReadOnly]  private int airJumpsLeft;
    
    [SerializeField] private float respawnInvulSeconds= 3;
    
    //states
    [SerializeField] private State state = new ();
    private bool CannotInput => !state.CanInput || !hasController;
    
    public Vector3 Velocity => rb.velocity;

    public static event Action<Character> OnCreated; 
    public static event Action<Character> OnDeath; 
    
    private Vector3 cachedVelocity;
    
    public MagicalGirlController controller;
    public bool hasController = false;
    private Dictionary<string,FrameDataSo.FrameData> frameDataDict;
    
    [Serializable]
    private class State
    {
        public bool grounded;
        public bool dropping;
        public bool ledged;
        
        public bool IsAttacking => totalFrames > 0;
        public int totalFrames => startup + active + recovering;
        public int startup;
        public int active;
        public int recovering;
        public bool Stunned => stunDuration > 0;
        public int maxStunDuration;
        public int stunDuration;
        public bool Invulnerable => invulFrames > 0;
        public int invulFrames;

        public bool ledgeJumped => jumpFrames > 0;
        public int jumpFrames;
        
        public bool CanInput => !Stunned && !IsAttacking && !dead;
        
        public bool dead;

        public void ResetStates()
        {
            grounded = false;
            ledged = false;
            dropping = false;
            
            maxStunDuration = 0;
            stunDuration = 0;
        
            startup = 0;
            active = 0;
            recovering = 0;
        
            invulFrames = 0;
        }
    }
    
    private void Start()
    {
        frameDataDict = frameDataSo.MakeDictionary();
        
        InitStats();
        
        OnCreated?.Invoke(this);
    }
    
    public void InitStats()
    {
        airJumpsLeft = maxAirJumps;
        
        gravityMultiplier = 1f;
        rb.velocity = Vector3.zero;
        
        state.ResetStates();
    }

    public void Respawn()
    {
        InitStats();
        state.dead = false;
        state.invulFrames = (int) (respawnInvulSeconds * 60);
    }
    
    public void Jump()
    {
        if(CannotInput) return;

        var canJump = state.grounded || (state.ledged && !ledgeJumpIsAirJump);

        if (!canJump) canJump = airJumpsLeft > 0;
        
        if(!canJump) return;
        
        airJumpsLeft--;
        
        cachedVelocity = rb.velocity;
        cachedVelocity.y = 0;
        rb.velocity = cachedVelocity;

        var dir = Vector3.up;
        if (state.ledged)
        {
            state.jumpFrames = ledgeJumpFrames;
            dir = Vector3.up + (Vector3.right * -controller.StickInput.x).normalized;
        }
        
        rb.AddForce(dir.normalized * jumpForce, ForceMode.Impulse);
    }

    public void Attack(bool heavy = false)
    {
        if(CannotInput) return;
        
        rb.velocity = Vector3.zero; //TODO do better
        
        var frameData = AttackUp(heavy);
        
        if (controller.StickInput != Vector2.zero)
        {
            var dot = Vector2.Dot(controller.StickInput, Vector2.up);
            var up = Mathf.Abs(1f-dot);
            var down = Mathf.Abs(-1f-dot);
            var side = Mathf.Abs(0-dot);

            if(up < down && up < side) frameData = AttackUp(heavy);
            else if(down < up && down < side) frameData = AttackDown(heavy);
            else
            {
                frameData = AttackSide(heavy);
            }
            
            FrameDataSo.FrameData AttackDown(bool heavyAttack)
            {
                return frameDataDict[state.grounded ? (heavyAttack ? "DownHeavy" : "DownLight") : (heavyAttack ? "GroundPound" : "DownAir")];
            }
        
            FrameDataSo.FrameData AttackSide(bool heavyAttack)
            {
                if (state.grounded)
                {
                    return frameDataDict[(heavyAttack ? "SideHeavy" : "SideLight")];
                }

                if (heavy)
                {
                    if(up < down) return AttackUp(true);
                    return AttackDown(true);
                }
            
                return frameDataDict["SideAir"];
            }
        }
        
        Debug.Log($"{frameData.AnimationName}");
        
        animator.Play(frameData.AnimationName);
        state.startup = frameData.Startup;
        state.active = frameData.Active;
        state.recovering = frameData.Recovery;
        
        return;
        
        FrameDataSo.FrameData AttackUp(bool heavyAttack)
        {
            return frameDataDict[state.grounded ? (heavyAttack ? "UpHeavy" : "UpLight") : (heavyAttack ? "Recovery" : "UpAir")];
        }
        
    }

    private void Update()
    {
        DecreaseStunDuration();
        DecreaseAttackFrames();
        DecreaseInvulFrames();
        DecreaseJumpFrames();
        Drop();
        CheckIsGrounded();
        CheckLedging();
    }

    private void CheckLedging()
    {
       if(CannotInput || state.grounded) return;
       
       if(controller.StickInput.x == 0) return;
       
       var dir = (Vector3.right * controller.StickInput.x).normalized;
       
       var rayDist = groundRange+groundCheckHeight;
       
       var ledgeHit = Physics.Raycast(transform.position - Vector3.up * 0.5f + dir * ((1-groundCheckHeight)*0.5f), dir, out var hit,
           rayDist,platformLayer);
       if (!ledgeHit)
           ledgeHit = Physics.Raycast(transform.position + Vector3.up * 0.5f + dir * ((1-groundCheckHeight)*0.5f), dir, out hit,
               rayDist,platformLayer);
       
       Debug.DrawRay(transform.position - Vector3.up * 0.5f + dir * ((1-groundCheckHeight)*0.5f), dir * rayDist, Color.red); 
       Debug.DrawRay(transform.position + Vector3.up * 0.5f + dir * ((1-groundCheckHeight)*0.5f), dir * rayDist, Color.red); 
       
       if(ledgeHit)
       {
           if (!state.ledged)
           {
               OnLedgeTouch();
           }
       }
       else if(state.grounded)
       {
           OnAirborne();
       }
    }

    private void Drop()
    {
        state.dropping = false;
        
        if(CannotInput) return;
        
        // TODO probably count frames
        
        if(controller.StickInput.y < 0) state.dropping = true;
    }

    private void CheckIsGrounded()
    {
        if(state.dead || state.Stunned) return;

        var mask = state.dropping ? platformLayerDrop : platformLayer;
        
        var rayDist = groundRange+groundCheckHeight;
        
        var groundHit = Physics.Raycast(transform.position - Vector3.right * 0.5f - Vector3.up * (1-groundCheckHeight), Vector3.down, out var hit,
            rayDist,mask);
        if (!groundHit)
            groundHit = Physics.Raycast(transform.position + Vector3.right * 0.5f - Vector3.up * (1-groundCheckHeight), Vector3.down, out hit,
                rayDist,mask);
        
        if(Velocity.y > 0) groundHit = false;
        if(groundHit)
        {
            if (!state.grounded)
            {
                OnTouchGround();
            }
        }
        else if(state.grounded || state.ledged)
        {
            OnAirborne();
        }
    }

    private void DecreaseJumpFrames()
    {
        if(!state.ledgeJumped) return;
        state.jumpFrames--;
    }

    private void DecreaseInvulFrames()
    {
        if(!state.Invulnerable) return;
        state.invulFrames--;
    }

    private void DecreaseAttackFrames()
    {
        if(!state.IsAttacking) return;
        if(state.startup > 0) state.startup--;
        else if(state.active > 0) state.active--;
        else if(state.recovering > 0) state.recovering--;
    }

    private void DecreaseStunDuration()
    {
        if(!state.Stunned) return;
        state.stunDuration--;
    }
    
    private void FixedUpdate()
    {
        UpdateMove();
        ApplyGravity();   
    }

    private void ApplyGravity()
    {
        rb.AddForce(Physics.gravity * (gravityMultiplier * rb.mass));
    }

    private void UpdateMove()
    {
        if(CannotInput) return;
        if(state.ledged || state.ledgeJumped) return;

        cachedVelocity = rb.velocity;
        cachedVelocity.x = controller.StickInput.x * runSpeed;
        rb.velocity = cachedVelocity;
    }

    private void Kill()
    {
        state.dead = true;
        InitStats();
        
        OnDeath?.Invoke(this);
    }

    public void OnLedgeTouch()
    {
        airJumpsLeft = maxAirJumps;
        state.ledged = true;
        gravityMultiplier = ledgeGravity;
    }
    
    public void OnTouchGround()
    {
        airJumpsLeft = maxAirJumps;
        state.grounded = true;
        gravityMultiplier = 0f;
        
        var inverseVel = Velocity;
        inverseVel.y *= -1;
        rb.AddForce(inverseVel, ForceMode.VelocityChange);
    }
    
    public void OnAirborne()
    {
        state.grounded = false;
        state.ledged = false;
        gravityMultiplier = 1f;
    }

    public void TakeHit(HitData data)
    {
        if(state.Invulnerable || state.dead) return;

        foreach (var go in hitboxes)
        {
            go.SetActive(false);
        }
        
        state.maxStunDuration = data.maxStunDuration;
        state.stunDuration += data.stunDuration;
        if(state.stunDuration > state.maxStunDuration) state.stunDuration = state.maxStunDuration;
        
        rb.velocity = Vector3.zero;
        rb.AddForce(data.direction * data.force, ForceMode.Impulse);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log($"{other.gameObject.name} entered trigger (layer {other.gameObject.layer})");
        switch (other.gameObject.layer)
        {
            case 9:
                Kill();
                break;
            default:
                break;
        }
    }
    
    
}
