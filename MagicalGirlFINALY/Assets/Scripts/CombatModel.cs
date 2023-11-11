using System.Collections.Generic;
using UnityEngine;

public class CombatModel : MonoBehaviour
{
    [field: SerializeField] public Animator Animator { get; private set; }
    [field: SerializeField] public FrameDataSo FrameData { get; private set; }
    
    [SerializeField] private List<GameObject> hitboxes = new ();

    public void ResetHitboxes()
    {
        foreach (var go in hitboxes)
        {
            go.SetActive(false);
        }
    }
    
    public void Show(bool value)
    {
        Animator.gameObject.SetActive(value);
    }
    
    
}
