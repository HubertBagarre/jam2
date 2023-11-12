using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public partial class Character : MonoBehaviour, ICameraFollow
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
    
    [SerializeField, ReadOnly] private float gravityMultiplier = 1f;

    [Header("Settings")] [SerializeField] private float runSpeed = 5f;
    [Space] 
    [SerializeField] private float ledgeGravity = 0.3f;
    [SerializeField] private int ledgeJumpFrames = 30;
    [SerializeField] private int ledgeFrames = 10;
    [Space] 
    [SerializeField] private int groundFrames = 10;
    [SerializeField] private int dropFrames = 10;
    [SerializeField] private int ShieldFrames = 10;
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
    public static event Action<float> OnTakeDamage;
    public event Action<int, int> OnPercentChanged;
    public event Action<float> OnTransformationChargeUpdated;
    public event Action<Character, float> OnGainUltimate;

    private Vector3 cachedVelocity;

    public MagicalGirlController controller;
    public bool hasController = false;
    private Dictionary<string, FrameDataSo.FrameData> frameDataDict;
    private bool OldTurnedLeft = true;
    
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

    public event Action<int> OnStartup;
    public event Action<int> OnActive;
    public event Action<int> OnRecovering;
    public bool OnActionTerminated = false;
    
    private bool firstTransform = true;

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
        public int totalActiveFrames => startup + active;
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
        public Vector3 ledgeDirection;

        public bool shielded => shieldFrames > 0;
        public int shieldFrames;


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
    
    public bool isInvulnerable()
    {
        return state.Invulnerable;
    }
    
    private void Update()
    {
        if(state.dead) return;
        
        DecreaseTransformedFrames();
        DecreaseStunDuration();
        DecreaseActionFrames();
        DecreaseInvulFrames();
        DecreaseJumpFrames();
        DecreaseLedgeFrames();
        DecreaseGroundFrames();
        DecreaseDropFrames();
        Drop();
        CheckIsGrounded();
        CheckLedging();
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

    private void DecreaseInvulFrames()
    {
        if (!state.Invulnerable) return;
        state.invulFrames--;
    }

    private void DecreaseActionFrames()
    {
        if (!state.IsActionPending) return;

        OnStartup?.Invoke(state.startup);
        OnActive?.Invoke(state.active);
        OnRecovering?.Invoke(state.recovering);
        
        if (state.startup > 0)
        {
            state.startup--;
            return;
        }
        
        if (state.active > 0)
        {

            state.active--;
            return;
        }
        
        if (state.recovering > 0)
        {
            if (OnActionTerminated)
            {
                OnActionTerminated = false;
                normalModel.ResetHitboxes();
                transformedModel.ResetHitboxes();
            }
            
            state.recovering--;
            if (state.recovering == 0)
            {
                OnActionTerminated = true;
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
        if(state.dead) return;
        
        UpdateMove();
        ApplyGravity();
        HandleAnimations();
    }

    private void ApplyGravity()
    {
        if (state.grounded) gravityMultiplier = 0f;
        if (state.ledged) gravityMultiplier = ledgeGravity;
        
        rb.AddForce(Physics.gravity * (gravityMultiplier * rb.mass));
    }

    private void UpdateMove()
    {
        CameraPosition = transform.position;
        
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

    public void UpdateColor(Color color)
    {
        normalModel.ChangeColor(color);
        transformedModel.ChangeColor(color);
    }

    private void OnLedgeTouch()
    {
        airJumpsLeft = maxAirJumps;
    }

    private void OnTouchGround()
    {
        state.grounded = true;
        airJumpsLeft = maxAirJumps;

        var inverseVel = Velocity;
        inverseVel.y *= -1;
        rb.AddForce(inverseVel, ForceMode.VelocityChange);
    }

    private void OnAirborne()
    {
        state.grounded = false;
        gravityMultiplier = 1f;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        switch (other.gameObject.layer)
        {
            case 9:
                Kill();
                break;
            default:
                break;
        }
    }

    public bool ShouldFollow { get; set; } = false;
    public Vector3 CameraPosition { get; private set; }
}