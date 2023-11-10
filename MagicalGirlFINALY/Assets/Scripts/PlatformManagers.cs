using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformManagers : MonoBehaviour
{
    [SerializeField] private Platform[] _platforms;
    
    void Start()
    {
        CameraController.OnDezoomEvent += MovePlatforms;
    }
    
    private void MovePlatforms(float speed)
    {
        foreach (var platform in _platforms)
            platform.MoveTo(speed);
    }
}
