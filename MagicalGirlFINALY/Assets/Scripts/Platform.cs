using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Platform : MonoBehaviour
{
    [SerializeField] private Vector3 finalPos;
    
    private List<Character> _colliders = new List<Character>();

    public void MoveTo(float speed)
    {
        transform.DOLocalMove(finalPos, speed);
    }

    private void OnCharacterCollide(Character ch)
    {
        ch.transform.position = new Vector3(ch.transform.position.x, transform.position.y + transform.lossyScale.y * 1.5f , ch.transform.position.z);
        _colliders.Add(ch);
        ch.OnTouchGround();
    }
    
    private void OnCharacterExit(Character ch)
    {
        if (_colliders.Contains(ch))
        {
            _colliders.Remove(ch);
            ch.OnAirborne();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.position.y < transform.position.y) return;
        Character ch = other.GetComponent<Character>();

        OnCharacterCollide(ch);
    }

    private void OnTriggerExit(Collider other)
    {
        Character ch = other.GetComponent<Character>();
        OnCharacterExit(ch);
    }
    
    private void OnCollisionEnter(Collision other)
    {
        /*if (other.transform.position.y < transform.position.y) return;
        Character ch = other.gameObject.GetComponent<Character>();
        OnCharacterCollide(ch);*/
    }
         
    private void OnCollisionExit(Collision other)
    {
        /*Character ch = other.gameObject.GetComponent<Character>();
        OnCharacterExit(ch);*/
    }
}
