using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AttackHitbox : MonoBehaviour
{
    public enum DirectionType { Up, Right, Forward, Custom}

    [HideInInspector] public bool hit;
    
    private Character own;
    
    [Header("Settings")]
    [SerializeField] private HitData hitData;

    public void Link(Character character)
    {
        own = character;
        GetComponent<Collider>().isTrigger = true;
        gameObject.layer = 7;
    }


    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other);
        
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
        if (!character.isInvulnerable())  hit = true;
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
    public int useVelocityDuration;
}
