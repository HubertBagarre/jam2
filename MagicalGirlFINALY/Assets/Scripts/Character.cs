using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Serialization;

public partial class Character : MonoBehaviour
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