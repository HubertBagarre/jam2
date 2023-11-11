using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Character own;
    
    [Header("Settings")]
    [SerializeField] private float damage = 1f;
    [SerializeField] private Vector3 direction;
    [SerializeField] private float force = 1f;
    [SerializeField] private int hitStunDuration = 10;
    
    private void OnTriggerEnter(Collider other)
    {
        var character = other.GetComponent<Character>();
        if(character == null) return;
        if(character == own) return;
        
        Debug.Log($"{transform.up}");
        Debug.DrawRay(transform.position,transform.up * force,Color.red, 1f);
        //character.Rb.AddForce(transform.up * force, ForceMode.Impulse);
    }
}
