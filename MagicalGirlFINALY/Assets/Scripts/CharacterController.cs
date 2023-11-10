using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody rb;
    
    [Header("Settings")]
    [SerializeField] private float speed = 5f;
    
    public void Move(InputAction.CallbackContext context)
    {
        var input = context.ReadValue<Vector2>();
        rb.velocity = input * speed;

    }
}
