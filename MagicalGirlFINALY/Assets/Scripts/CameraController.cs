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
    [SerializeField] private float minPosCam = -10f;
    [SerializeField] private float delayDezoom = 0.01f;
    [SerializeField] private List<Transform> _targets;
    [SerializeField] private Transform centerOfWorld;
    [SerializeField] private float maxDistanceFromCenter = 10f;
    [SerializeField] private float speed = 0.1f;

    private List<Renderer> _renderers = new List<Renderer>();
    
    private Vector3 expectedPos;


    private void OnDezoom()
    {
        OnDezoomEvent?.Invoke(delayDezoom);
        transform.DOMoveZ(maxPosCam, delayDezoom);
    }

    private void Start()
    {
        GameManager.newPlayerSpawned += AddTarget;
    }

    private void AddTarget(Transform _target)
    {
        _targets.Add(_target);
        _renderers.Add(_target.GetComponent<Renderer>());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            OnDezoom();
        
        Vector2 median = centerOfWorld.position;
        var worldPos = median;
        foreach (var target in _targets)
        {
            median += (Vector2)target.position;
        }
        
        median /= _targets.Count + 1;

        var currentPos = _camera.transform.position;
        expectedPos = new Vector3(median.x, median.y, currentPos.z);
        
        var dir = (Vector2)expectedPos - worldPos;
        
        if(dir.magnitude > maxDistanceFromCenter) expectedPos = worldPos + dir.normalized * maxDistanceFromCenter;

        expectedPos.z = currentPos.z;
        
        transform.position = Vector3.Lerp(transform.position, expectedPos, speed);
    }
}