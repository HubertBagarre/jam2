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
    
    private float targetSpeed = 0f;
    
    private Vector3 cachedVelocity;
    
    public void Move(Vector2 direction)
    {
        targetSpeed = direction.x * speed;
    }

    public void Jump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    public void Attack()
    {
        animator.Play("Attack");
    }

    private void Update()
    {
        
    }
    
    private void FixedUpdate()
    {
        cachedVelocity = rb.velocity;
        cachedVelocity.x = targetSpeed;
        rb.velocity = cachedVelocity;
    }
}
