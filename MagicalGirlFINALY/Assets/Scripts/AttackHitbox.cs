using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Character own;
    
    [Header("Settings")]
    [SerializeField] private HitData hitData;
    
    private void OnTriggerEnter(Collider other)
    {
        var character = other.GetComponent<Character>();
        if(character == null) return;
        if(character == own) return;
        
        hitData.direction = transform.up;
        
        Debug.Log($"{transform.up}");
        Debug.DrawRay(transform.position,transform.up * hitData.force,Color.red, 1f);
        character.TakeHit(hitData);
    }
}

[Serializable]
public struct HitData
{
    public int stunDuration;
    [HideInInspector] public Vector3 direction;
    public float force;
    public float damage;
    public int maxStunDuration;
}
