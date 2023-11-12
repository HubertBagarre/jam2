using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Character Data")]
public class FrameDataSo : ScriptableObject
{
    [field: SerializeField] public RuntimeAnimatorController AnimatorController { get; private set; }
    [field: SerializeField] public List<FrameData> frameData;
    
    public Dictionary<string,FrameData> MakeDictionary()
    {
        var dict = new Dictionary<string, FrameData>();
        foreach (var frame in frameData)
        {
            dict.Add(frame.AnimationName,frame);
        }

        return dict;
    }
    
    [Serializable]
    public class FrameData
    {
        [field: SerializeField] public string AnimationName { get; private set; }
        [field: SerializeField] public int Startup { get; private set; }
        [field: SerializeField] public int Active { get; private set; }
        [field: SerializeField] public int Recovery { get; private set; }
        [field: SerializeField] public bool StopVelocityX { get; private set; }
        [field: SerializeField] public bool StopVelocityY { get; private set; }
    }
}
