using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour,ICameraFollow
{
    public bool ShouldFollow => true;
    public Vector3 CameraPosition => transform.position;
}

public interface ICameraFollow
{
    public bool ShouldFollow { get; }
    public Vector3 CameraPosition { get; }
}
