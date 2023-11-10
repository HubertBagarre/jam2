using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
[SerializeField] private Camera _camera;
[SerializeField] private float maxSizeCam = 12f;
[SerializeField] private float maxPosCam = 12f;
[SerializeField] private float speedDezoom = 0.1f;
[SerializeField] private float delayDezoom = 0.01f;

IEnumerator OnDezoom()
{
    float ratio  = maxPosCam - transform.position.y;
    ratio /= (maxSizeCam - _camera.orthographicSize) / speedDezoom;
    
    while (_camera.fieldOfView < maxSizeCam)
    {
        _camera.fieldOfView += speedDezoom;
        transform.position = new Vector3(transform.position.x, transform.position.y + ratio, transform.position.z);
        yield return new WaitForSeconds(delayDezoom);
    }
}
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            StartCoroutine(OnDezoom());
    }
}
