using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Serialization;

public class Character : MonoBehaviour
{
    [Header("Components")] [SerializeField]
    private Rigidbody rb;

    [field: SerializeField] public Transform ModelParent { get; private set; }

    private CombatModel normalModel;
    private CombatModel transformedModel;
    private CombatModel CurrentBattleModel => state.shouldBeTransformed ? transformedModel : normalModel;
    private Animator NormalAnimator => normalModel.Animator;
    private Animator TransformedAnimator => transformedModel.Animator;
    private Animator CurrentAnimator => CurrentBattleModel.Animator;
    private FrameDataSo CurrentFrameData => CurrentBattleModel.FrameData;
    [SerializeField] private List<GameObject> hitboxes;
    [SerializeField, ReadOnly] private float gravityMultiplier = 1f;

    [Header("Settings")] [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float ledgeGravity = 0.3f;
    [Space] [SerializeField] private int ledgeJumpFrames = 30;
    [SerializeField] private int ledgeFrames = 10;
    [SerializeField] private int groundFrames = 10;
    [SerializeField] private int dropFrames = 10;
    [SerializeField] private int ShieldFrames = 10;
    [SerializeField] private int DashFrames = 10;
    [SerializeField] private int cooldownFrameReloadShield = 10;
    [SerializeField] private int cooldownFrameReloadDash = 10;
    [SerializeField] private int transformationFrames = 60;

    [Space] [SerializeField] private float groundRange = 0.1f;
    [SerializeField] private float groundCheckHeight = 0.1f;
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private LayerMask platformLayerDrop;
    [Space] [SerializeField] private float jumpForce = 5f;
    [Space] [SerializeField] private float dashForce = 5f;
    [SerializeField] private int maxAirJumps = 2;
    [SerializeField] private bool ledgeJumpIsAirJump = true;

    [SerializeField, ReadOnly] private int airJumpsLeft;

    [SerializeField] private float respawnInvulSeconds = 3;

    //states
    [SerializeField] private State state = new();
    private bool CannotInput => !state.CanInput || !hasController;
    private Vector3 endedPositionDashRatio;

    public Vector3 Velocity => rb.velocity;

    public static event Action<Character> OnCreated;
    public static event Action<Character> OnDeath;
    public event Action<int, int> OnPercentChanged;
    public event Action<float> OnTransformationChargeUpdated;
    public event Action<Character, float> OnGainUltimate;

    private Vector3 cachedVelocity;

    public MagicalGirlController controller;
    public bool hasController = false;
    private Dictionary<string, FrameDataSo.FrameData> frameDataDict;
    private bool OldTurnedLeft = true;

    private int cooldownShield = 0;
    private int cooldownDash = 0;
    private bool OnCooldownShield => cooldownShield > 0;
    private bool OnCooldownDash => cooldownDash > 0;
    private float CumulDamage;
    [SerializeField] private float chargeUltimateLight = 0.1f;
    [SerializeField] private float chargeUltimateHeavy = 0.2f;

    [SerializeField] private int useVelocityFrames = 0;
    [SerializeField] private bool hasMoved = false;

    private static readonly int animCanInput = Animator.StringToHash("canInput");
    private static readonly int animIsGrounded = Animator.StringToHash("isGrounded");
    private static readonly int animIsLedged = Animator.StringToHash("isLedged");
    private static readonly int animIsDropping = Animator.StringToHash("isDropping");
    private static readonly int animVelocityX = Animator.StringToHash("velocityX");
    private static readonly int animMagnitudeX = Animator.StringToHash("magnitudeX");
    private static readonly int animVelocityY = Animator.StringToHash("velocityY");

    [SerializeField, ReadOnly] private float CumulUltimate = 0;

    private float lastAttackChargeUltimate = 0;

    public event Action OnStartupEnd;
    public event Action OnActiveEnd;
    public event Action OnRecoveringEnd;
    public bool OnActionTerminated = false;

    [Serializable]
    private class State
    {
        public bool isTransformed;
        public bool shouldBeTransformed => transformedFrames > 0;
        public int transformedFrames;

        public bool grounded;
        public int groundFrames;

        public bool dropping => dropFrames > 0;
        public int dropFrames;

        public bool IsActionPending => totalFrames > 0;
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

        public bool ledged => ledgeFrames > 0;
        public int ledgeFrames;

        public bool shielded => shieldFrames > 0;
        public int shieldFrames;

        public bool dashed => dashFrames > 0;
        public int dashFrames;

        public bool CanInput => !Stunned && !IsActionPending && !dead;

        public bool dead;

        public void ResetStates()
        {
            isTransformed = false;
            transformedFrames = 0;

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
        InitStats();

        OnCreated?.Invoke(this);
    }

    public void InitStats()
    {
        if (CurrentFrameData) frameDataDict = CurrentFrameData.MakeDictionary();
        
        normalModel.ResetHitboxes();
        transformedModel.ResetHitboxes();

        state.grounded = false;
        airJumpsLeft = maxAirJumps;

        gravityMultiplier = 1f;
        rb.velocity = Vector3.zero;

        state.ResetStates();
        CumulDamage = 0;
        OnPercentChanged?.Invoke(0, 0);
        OnGainUltimate?.Invoke(this, 0f);

        useVelocityFrames = 0;
        hasMoved = false;
        
        normalModel.Show(true);
        transformedModel.Show(false);
    }

    public void ApplyPlayerOptions(GameManager.PlayerOptions options)
    {
        normalModel = Instantiate(options.NormalModel, ModelParent);
        normalModel.gameObject.name = "NormalModel";

        transformedModel = Instantiate(options.TransformedModel, ModelParent);
        transformedModel.gameObject.name = "TransformedModel";

        Transformation(false);
    }

    [ContextMenu("Transform (true)")]
    private void TransformT()
    {
        Transformation(true);
    }

    [ContextMenu("Transform (false)")]
    private void TransformF()
    {
        Transformation(false);
    }

    public void Transformation(bool transformed)
    {
        Debug.Log($"Transformation : {transformed}");
        
        
        if (CurrentFrameData) frameDataDict = CurrentFrameData.MakeDictionary();
        
        frameDataDict.TryGetValue("Transformation", out var frameData);
        
        OnActiveEnd += SwitchModel;
        
        TransformedAnimator.CrossFade(frameData.AnimationName, 0.1f);
        NormalAnimator.CrossFade(frameData.AnimationName, 0.1f);
        
        state.startup = frameData.Startup;
        state.active = frameData.Active;
        state.recovering = frameData.Recovery;
        if (transformed) state.transformedFrames = transformationFrames + state.totalFrames;
        
        return;
        
        void SwitchModel()
        {
            normalModel.Show(!transformed);
            transformedModel.Show(transformed);
            state.isTransformed = transformed;
        }
    }

    public void Respawn()
    {
        InitStats();
        state.dead = false;
        state.invulFrames = (int)(respawnInvulSeconds * 60);
    }

    public void Shield()
    {
        Debug.Log("Shield");
        if (CannotInput || OnCooldownShield) return;
        state.shieldFrames = ShieldFrames;
        frameDataDict.TryGetValue("Shield", out var frameData);
        cooldownShield = cooldownFrameReloadShield + ShieldFrames;
        
        PlayAnimation(frameData,0.05f);

        cooldownShield += state.startup + state.active + state.recovering;
    }

    public void Dash()
    {
        if (CannotInput || OnCooldownDash) return;
        state.dashFrames = DashFrames;
        frameDataDict.TryGetValue("Dash", out var frameData);
        cooldownDash = cooldownFrameReloadDash + DashFrames;
        Vector2 dir = controller.StickInput;
        endedPositionDashRatio = new Vector3(dir.x, dir.y, 0) * dashForce;

        endedPositionDashRatio /= DashFrames;

        PlayAnimation(frameData,0.05f);

        cooldownDash += state.startup + state.active + state.recovering;
    }

    public void Jump()
    {
        if (CannotInput) return;

        var grounded = state.grounded || (state.ledged && !ledgeJumpIsAirJump);

        if (!grounded && airJumpsLeft <= 0) return;

        if (!grounded) airJumpsLeft--;

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
        if (CannotInput) return;

        //rb.velocity = Vector3.zero; //TODO do better

        var frameData = AttackUp(heavy);

        if (controller.StickInput != Vector2.zero)
        {
            var dot = Vector2.Dot(controller.StickInput, Vector2.up);
            var up = Mathf.Abs(1f - dot);
            var down = Mathf.Abs(-1f - dot);
            var side = Mathf.Abs(0 - dot);

            if (up < down && up < side) frameData = AttackUp(heavy);
            else if (down < up && down < side) frameData = AttackDown(heavy);
            else
            {
                frameData = AttackSide(heavy);
            }

            FrameDataSo.FrameData AttackDown(bool heavyAttack)
            {
                return frameDataDict[
                    state.grounded
                        ? (heavyAttack ? "DownHeavy" : "DownLight")
                        : (heavyAttack ? "GroundPound" : "DownAir")];
            }

            FrameDataSo.FrameData AttackSide(bool heavyAttack)
            {
                if (state.grounded)
                {
                    return frameDataDict[(heavyAttack ? "SideHeavy" : "SideLight")];
                }

                if (heavy)
                {
                    if (up < down) return AttackUp(true);
                    return AttackDown(true);
                }

                return frameDataDict["SideAir"];
            }
        }
        
        rb.velocity = new Vector3(
            (frameData.StopVelocityX) ? 0 : rb.velocity.x,
            (frameData.StopVelocityY) ? 0 : rb.velocity.y,
            rb.velocity.z);

        PlayAnimation(frameData);
        
        lastAttackChargeUltimate = (heavy ? chargeUltimateHeavy : chargeUltimateLight);
        
        return;

        FrameDataSo.FrameData AttackUp(bool heavyAttack)
        {
            return frameDataDict[
                state.grounded ? (heavyAttack ? "UpHeavy" : "UpLight") : (heavyAttack ? "Recovery" : "UpAir")];
        }
    }

    private void PlayAnimation(FrameDataSo.FrameData frameData,float transitionDuration = 0.1f)
    {
        if (frameData == null) return;
        
        var str = frameData.AnimationName;
        Debug.Log($"Playing {str} data on {CurrentBattleModel}");
        
        CurrentAnimator.CrossFade(frameData.AnimationName, transitionDuration);

        state.startup = frameData.Startup;
        state.active = frameData.Active;
        state.recovering = frameData.Recovery;
    }

    private void Update()
    {
        DecreaseTransformedFrames();
        DecreaseStunDuration();
        DecreaseAttackFrames();
        DecreaseInvulFrames();
        DecreaseJumpFrames();
        DecreaseLedgeFrames();
        DecreaseGroundFrames();
        DecreaseDropFrames();
        DecreaseActivationShieldFrames();
        DecreaseCooldownShieldFrames();
        DecreaseActivationDashFrames();
        DecreaseCooldownDashFrames();
        Drop();
        CheckIsGrounded();
        CheckLedging();
    }

    private void DecreaseTransformedFrames()
    {
        if (!state.shouldBeTransformed && state.isTransformed && !CannotInput)
        {
            Transformation(false);
        }
        if (!state.shouldBeTransformed) return;
        state.transformedFrames--;
        OnTransformationChargeUpdated?.Invoke(CumulUltimate);
        
        //TODO decrease cumul ultimate
    }

    private void CheckLedging()
    {
        if (CannotInput || state.grounded) return;

        if (controller.StickInput.x == 0) return;

        if (state.ledged && controller.StickInput.y == 0) return;

        var dir = (Vector3.right * controller.StickInput.x).normalized;

        var rayDist = groundRange + groundCheckHeight;

        var ledgeHit = Physics.Raycast(transform.position - Vector3.up * 0.5f + dir * ((1 - groundCheckHeight) * 0.5f),
            dir, out var hit,
            rayDist, platformLayer);
        if (!ledgeHit)
            ledgeHit = Physics.Raycast(transform.position + Vector3.up * 0.5f + dir * ((1 - groundCheckHeight) * 0.5f),
                dir, out hit,
                rayDist, platformLayer);

        if (ledgeHit)
        {
            state.ledgeFrames = ledgeFrames;
            if (!state.ledged)
            {
                OnLedgeTouch();
            }
        }
        else if (state.grounded)
        {
            OnAirborne();
        }
    }

    private void Drop()
    {
        if (CannotInput) return;

        if (controller.StickInput.y < -0.5f) state.dropFrames = dropFrames;
    }

    private void CheckIsGrounded()
    {
        if (state.dead || state.Stunned) return;

        var mask = state.dropping ? platformLayerDrop : platformLayer;

        var rayDist = groundRange + groundCheckHeight;

        var groundHit = Physics.Raycast(
            transform.position - Vector3.right * 0.5f - Vector3.up * (1 - groundCheckHeight), Vector3.down, out var hit,
            rayDist, mask);
        if (!groundHit)
            groundHit = Physics.Raycast(
                transform.position + Vector3.right * 0.5f - Vector3.up * (1 - groundCheckHeight), Vector3.down, out hit,
                rayDist, mask);

        if (Velocity.y > 0) groundHit = false;
        if (groundHit)
        {
            state.groundFrames = groundFrames;
            if (!state.grounded)
            {
                OnTouchGround();
            }
        }
        else if (state.grounded || state.ledged)
        {
            OnAirborne();
        }
    }

    private void DecreaseJumpFrames()
    {
        if (!state.ledgeJumped) return;
        state.jumpFrames--;
    }

    private void DecreaseLedgeFrames()
    {
        if (!state.ledged) return;
        state.ledgeFrames--;
    }

    private void DecreaseGroundFrames()
    {
        if (!state.grounded) return;
        state.groundFrames--;
    }

    private void DecreaseDropFrames()
    {
        if (!state.dropping) return;
        state.dropFrames--;
    }


    private void DecreaseActivationShieldFrames()
    {
        if (!state.shielded) return;
        state.shieldFrames--;
    }

    private void DecreaseCooldownShieldFrames()
    {
        if (!OnCooldownShield) return;
        cooldownShield--;
    }

    private void DecreaseActivationDashFrames()
    {
        if (!state.dashed) return;
        state.dashFrames--;
        if (!Physics.Raycast(transform.position, endedPositionDashRatio, out var hit, 1f))
            rb.MovePosition(transform.position + endedPositionDashRatio);
    }

    private void DecreaseCooldownDashFrames()
    {
        if (!OnCooldownDash) return;
        cooldownDash--;
    }

    private void DecreaseInvulFrames()
    {
        if (!state.Invulnerable) return;
        state.invulFrames--;
    }

    private void DecreaseAttackFrames()
    {
        if (!state.IsActionPending) return;

        if (state.startup > 0)
        {
            state.startup--;
            return;
        }
        
        if (state.active > 0)
        {
            if (!OnActionTerminated)
            {
                OnActionTerminated = true;
                OnStartupEnd?.Invoke();
                OnStartupEnd = null;
            }

            state.active--;
            return;
        }
        
        if (state.recovering > 0)
        {
            if (OnActionTerminated)
            {
                OnActionTerminated = false;
                
                if (CurrentBattleModel.HitThisFrame()) GainUltimate(lastAttackChargeUltimate, true);
                
                OnActiveEnd?.Invoke();
                normalModel.ResetHitboxes();
                transformedModel.ResetHitboxes();
                
                OnActiveEnd = null;
            }
            
            state.recovering--;
            
            if(state.recovering > 0) return;
            
            if (!OnActionTerminated)
            {
                OnActionTerminated = true;
                OnRecoveringEnd?.Invoke();
                OnRecoveringEnd = null;
            }
        }
    }

    private void DecreaseStunDuration()
    {
        if (!state.Stunned) return;
        state.stunDuration--;
    }

    private void FixedUpdate()
    {
        UpdateMove();
        ApplyGravity();
        HandleAnimations();
    }

    private void ApplyGravity()
    {
        rb.AddForce(Physics.gravity * (gravityMultiplier * rb.mass));
    }

    private void UpdateMove()
    {
        if (useVelocityFrames > 0) useVelocityFrames--;

        if (CannotInput) return;

        if (state.ledged || state.ledgeJumped) return;
        
        if (controller.StickInput.x != 0) hasMoved = true;

        if (useVelocityFrames > 0 && !hasMoved) return;

        cachedVelocity = rb.velocity;
        cachedVelocity.x = controller.StickInput.x * runSpeed;
        rb.velocity = cachedVelocity;
        Vector3 eulerAngle = transform.eulerAngles;
        if (controller.StickInput.x < 0)
        {
            eulerAngle.y = -90;
        }
        else if (controller.StickInput.x > 0)
        {
            eulerAngle.y = 90;
        }

        transform.eulerAngles = eulerAngle;
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
        gravityMultiplier = ledgeGravity;
    }

    public void OnTouchGround()
    {
        state.grounded = true;
        airJumpsLeft = maxAirJumps;
        gravityMultiplier = 0f;

        var inverseVel = Velocity;
        inverseVel.y *= -1;
        rb.AddForce(inverseVel, ForceMode.VelocityChange);
    }

    public void OnAirborne()
    {
        state.grounded = false;
        gravityMultiplier = 1f;
    }

    public void TakeHit(HitData data)
    {
        if (state.Invulnerable || state.dead || state.shielded) return;
        var prev = (int)CumulDamage;
        CumulDamage += data.damage;
        OnPercentChanged?.Invoke(prev, (int)CumulDamage);
        foreach (var go in hitboxes)
        {
            go.SetActive(false);
        }

        state.maxStunDuration = data.maxStunDuration;
        state.stunDuration += data.stunDuration;
        if (state.stunDuration > state.maxStunDuration) state.stunDuration = state.maxStunDuration;

        rb.velocity = Vector3.zero;

        CurrentAnimator.Play("Hit");
        normalModel.ResetHitboxes();
        transformedModel.ResetHitboxes();

        var force = data.force;
        if (!data.fixedForce) force *= CumulDamage * 0.01f;

        rb.AddForce(data.direction * force, ForceMode.VelocityChange); //multiply by percentDamage

        useVelocityFrames = data.useVelocityDuration;
        hasMoved = false;
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

    private void HandleAnimations()
    {
        NormalAnimator.SetBool(animCanInput, !CannotInput);
        NormalAnimator.SetBool(animCanInput, !CannotInput);
        NormalAnimator.SetBool(animIsGrounded, state.grounded);
        NormalAnimator.SetBool(animIsLedged, state.ledged);
        NormalAnimator.SetBool(animIsDropping, state.dropping);
        NormalAnimator.SetFloat(animVelocityX, Velocity.x);
        NormalAnimator.SetFloat(animMagnitudeX, Mathf.Abs(Velocity.x));
        NormalAnimator.SetFloat(animVelocityY, Velocity.y);
        
        TransformedAnimator.SetBool(animCanInput, !CannotInput);
        TransformedAnimator.SetBool(animCanInput, !CannotInput);
        TransformedAnimator.SetBool(animIsGrounded, state.grounded);
        TransformedAnimator.SetBool(animIsLedged, state.ledged);
        TransformedAnimator.SetBool(animIsDropping, state.dropping);
        TransformedAnimator.SetFloat(animVelocityX, Velocity.x);
        TransformedAnimator.SetFloat(animMagnitudeX, Mathf.Abs(Velocity.x));
        TransformedAnimator.SetFloat(animVelocityY, Velocity.y);
    }

    public float GainUltimate(float percent, bool isMine = false)
    {
        if (state.shouldBeTransformed) return CumulUltimate;

        CumulUltimate += percent;
        if (isMine) OnGainUltimate?.Invoke(this, percent);
        OnTransformationChargeUpdated?.Invoke(CumulUltimate);

        if (CumulUltimate >= 1)
        {
            Transformation(true);
        }

        return CumulUltimate;
    }
}