using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CombatModel : MonoBehaviour
{
    [field: SerializeField] public Animator Animator { get; private set; }
    [field: SerializeField] public FrameDataSo FrameData { get; private set; }
    [field: SerializeField] public List<Transform> Foots { get; private set; }

    [SerializeField] private List<GameObject> objectsToShow;
    [SerializeField] private List<AttackHitbox> hitboxes = new ();

    public void ResetHitboxes()
    {
        foreach (var go in hitboxes)
        {
            go.gameObject.SetActive(false);
            go.hit = false;
        }
    }

    public bool HitThisFrame()
    {
        return hitboxes.Any(hitbox => hitbox.hit);
    }
    
    
    public void Show(bool value)
    {
        foreach (var modelChildGo in objectsToShow)
        {
            modelChildGo.SetActive(value);
        }
    }
    
    
}
