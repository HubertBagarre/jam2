using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static event Action<float> OnDezoomEvent;
    
[SerializeField] private Camera _camera;
[SerializeField] private float maxPosCam = 12f;
[SerializeField] private float delayDezoom = 0.01f;

private void OnDezoom()
{
    OnDezoomEvent?.Invoke(delayDezoom);
    transform.DOMoveZ(maxPosCam, delayDezoom);
}
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            OnDezoom();
    }
}
