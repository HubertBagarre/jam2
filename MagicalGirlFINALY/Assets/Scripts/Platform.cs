using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Platform : MonoBehaviour
{
    [SerializeField] private Vector3 finalPos;
    
    private List<Rigidbody> _colliders = new List<Rigidbody>();

    public void MoveTo(float speed)
    {
        transform.DOLocalMove(finalPos, speed);
    }

    private void Update()
    {
        foreach (var t in _colliders)
        {
            var velocity = t.velocity;
            velocity = new Vector3(velocity.x, 0.3f, velocity.z);
            t.velocity = velocity;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.position.y < transform.position.y) return;
        Rigidbody rg = other.GetComponent<Rigidbody>();
        other.transform.position = new Vector3(other.transform.position.x, transform.position.y + transform.lossyScale.y , other.transform.position.z);
        rg.velocity = new Vector3(rg.velocity.x, 0, rg.velocity.z);
        _colliders.Add(rg);
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody rg = other.GetComponent<Rigidbody>();

        if (_colliders.Contains(rg))
        {
            _colliders.Remove(rg);
        }
    }
}
