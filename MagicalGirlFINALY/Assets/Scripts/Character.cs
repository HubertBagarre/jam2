using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Animator animator;
    
    [Header("Settings")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private int maxAirJumps = 2;
    [SerializeField] private bool ledgeJumpIsAirJump = true;

    private int jumpsLeft;
    
    //states
    private State state = new State();

    public static event Action<Character> OnCreated; 
    public static event Action<Character> OnDeath; 
    
    private float targetSpeed = 0f;
    
    private Vector3 cachedVelocity;
    
    private class State
    {
        public bool grounded;
        public bool stunned;
        
        public bool startup;
        public bool attacking;
        public bool recovering;
        public bool CanInput => !stunned && !startup && !attacking && !recovering && !dead;
        
        public bool dead;
    }
    
    private void Start()
    {
        InitStats();
        
        OnCreated?.Invoke(this);
    }

    public void InitStats()
    {
        jumpsLeft = maxAirJumps;
    }
    
    public void Move(Vector2 direction)
    {
        targetSpeed = direction.x * speed;
    }

    public void Jump()
    {
        if(jumpsLeft <= 0) return;
        
        if(!state.grounded) jumpsLeft--;
        
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    public void Attack()
    {
        animator.Play("Attack");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
           Jump();
        
        if (Input.GetKeyDown(KeyCode.D))
            rb.velocity = new Vector3(5, 0, 0);
        
        if (Input.GetKeyDown(KeyCode.Q))
            rb.velocity = new Vector3(-5, 0, 0);
    }
    
    private void FixedUpdate()
    {
        cachedVelocity = rb.velocity;
        cachedVelocity.x = targetSpeed;
        rb.velocity = cachedVelocity;
    }

    private void Kill()
    {
        OnDeath?.Invoke(this);
        Debug.Log("OOF");
    }
    
    public void OnTouchGround()
    {
        jumpsLeft = maxAirJumps;
        state.grounded = true;
        rb.useGravity = false;
        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z); 
    }
    
    public void OnAirborne()
    {
        state.grounded = false;
        rb.useGravity = true;
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
