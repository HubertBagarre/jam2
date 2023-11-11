using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    public enum DirectionType { Up, Right, Forward, Custom}
    
    [Header("Components")]
    [SerializeField] private Character own;
    
    [Header("Settings")]
    [SerializeField] private HitData hitData;
    
    private void OnTriggerEnter(Collider other)
    {
        var character = other.GetComponent<Character>();
        if(character == null) return;
        if(character == own) return;

        var tr = transform;
        hitData.direction = hitData.directionType switch
        {
            DirectionType.Up => tr.up,
            DirectionType.Right => tr.right,
            DirectionType.Forward => tr.forward,
            DirectionType.Custom => hitData.direction,
            _ => hitData.direction
        };
        
        Debug.DrawRay(tr.position,hitData.direction * hitData.force,Color.red, 1f);
        character.TakeHit(hitData);
    }
}

[Serializable]
public struct HitData
{
    public int stunDuration;
    public AttackHitbox.DirectionType directionType;
    public Vector3 direction;
    public float force;
    public bool fixedForce;
    public float damage;
    public int maxStunDuration;
}
