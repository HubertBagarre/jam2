using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CombatModel : MonoBehaviour
{
    [field: SerializeField] public Animator Animator { get; private set; }
    [field: SerializeField] public FrameDataSo FrameData { get; private set; }
    [field: SerializeField] public List<Renderer> ColoredRenderers { get; private set; } 
    [field: SerializeField] public List<Transform> Foots { get; private set; }
    [field: SerializeField] public Transform Body { get; private set; }

    [SerializeField] private List<GameObject> objectsToShow;
    
    [SerializeField] private List<GameObject> fx = new ();
    [SerializeField] private List<AttackHitbox> hitboxes = new ();

    public void ChangeColor(Color color)
    {
        foreach (var rend in ColoredRenderers)
        {
            var mat = rend.material;
            mat.color = color;
            rend.material = mat;
        }
    }
    
    public void ResetHitboxes()
    {
        foreach (var go in hitboxes)
        {
            go.gameObject.SetActive(false);
            go.hit = false;
        }
    }
    
    public void ResetFx()
    {
        foreach (var go in fx)
        {
            go.SetActive(false);
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
