using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody rb;
    
    [Header("Settings")]
    [SerializeField] private float speed = 5f;
    
    public void Move(Vector2 direction)
    {
        rb.velocity = direction.x * speed * Vector3.right;
    }
}
