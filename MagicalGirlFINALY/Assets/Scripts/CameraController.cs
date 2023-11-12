using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static event Action<float> OnDezoomEvent;

    [SerializeField] private Camera _camera;
    [SerializeField] private float maxPosCam = 12f;
    [SerializeField] private float minPosCam = -10f;
    [SerializeField] private float delayDezoom = 0.01f;
    [SerializeField] private CameraFollow centerOfWorld;
    private List<ICameraFollow> _targets = new List<ICameraFollow>();
    [SerializeField] private float maxDistanceFromCenter = 10f;
    [SerializeField] private float speed = 0.1f;
    
    private Vector3 expectedPos;


    private void OnDezoom()
    {
        OnDezoomEvent?.Invoke(delayDezoom);
        transform.DOMoveZ(maxPosCam, delayDezoom);
    }

    private void Start()
    {
        Character.OnCreated += AddTarget;
        GameManager.OnFirstUltiProc += OnDezoom;
    }

    private void AddTarget(Character _target)
    {
        _targets.Add(_target);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            OnDezoom();
        
        Vector2 median = centerOfWorld.CameraPosition;
        var worldPos = median;
        if (_targets.Any(target => target.ShouldFollow))
        {
            foreach (var target in _targets.Where(target => target.ShouldFollow))
            {
                median += (Vector2)target.CameraPosition;
            }
            median /= _targets.Count + 1;
        }
        
        var currentPos = _camera.transform.position;
        expectedPos = new Vector3(median.x, median.y, currentPos.z);
        
        var dir = (Vector2)expectedPos - worldPos;
        
        if(dir.magnitude > maxDistanceFromCenter) expectedPos = worldPos + dir.normalized * maxDistanceFromCenter;

        expectedPos.z = currentPos.z;
        
        transform.position = Vector3.Lerp(transform.position, expectedPos, speed);
    }
}