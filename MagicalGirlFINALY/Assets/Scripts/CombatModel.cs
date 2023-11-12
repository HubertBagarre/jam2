using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class CombatModel : MonoBehaviour
{
    [field: SerializeField] public Animator Animator { get; private set; }
    [field: SerializeField] public FrameDataSo FrameData { get; private set; }
    [field: SerializeField] public List<Renderer> ColoredRenderers { get; private set; }
    [field: SerializeField] public List<Transform> Foots { get; private set; }
    [field: SerializeField] public Transform Body { get; private set; }

    [SerializeField] private List<GameObject> objectsToShow;

    [SerializeField] private List<GameObject> fx = new();
    [SerializeField] private List<AttackHitbox> hitboxes = new();

    [FormerlySerializedAs("renderer")]
    [Header("Renderer")]
    [SerializeField] private List<Renderer> modifiedRenderers;
    private Dictionary<Renderer,Material> normalMaterials = new ();
    [SerializeField] private Material InvulnerabilityMaterial;
    [SerializeField] private Material HitMaterial;
    public int ratioChangeColor { get; private set; }

    private void Start()
    {
        ratioChangeColor = 16;
    }

    public void ChangeColor(Color color)
    {
        foreach (var rend in ColoredRenderers)
        {
            var mat = rend.material;
            mat.color = color;
            rend.material = mat;
        }
    }

    public void changeRimLightInvulnerability(bool isInvulnerable)
    {
        InvulnerabilityMaterial.SetFloat("_RimPower", isInvulnerable ? 1 : 0);
    }

    public void ChangeMaterialInvulnerability(bool isInvulnerable)
    {
        foreach (var rend in modifiedRenderers)
        {
            if (!isInvulnerable) SetNormalMaterial(rend);
            rend.material = isInvulnerable ? InvulnerabilityMaterial : normalMaterials[rend];
        }
        
    }

    private void SetNormalMaterial(Renderer rend)
    {
        if(normalMaterials.ContainsKey(rend)) return;
        normalMaterials.Add(rend,rend.material);
    }
    
    public void ChangeMaterialHit(bool isHit)
    {
        foreach (var rend in modifiedRenderers)
        {
            if (!isHit) SetNormalMaterial(rend);
            rend.material = isHit ? HitMaterial : normalMaterials[rend];
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