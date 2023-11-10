using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Platform : MonoBehaviour
{
    [SerializeField] private Vector3 finalPos;

    public void MoveTo(float speed)
    {
        transform.DOLocalMove(finalPos, speed);
    }
}
