using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    Vector3 originalScale;
    [SerializeField] private Vector3 scaleMultiplier = new Vector3(1.2f,1.2f,1);
    private Vector3 scale;
    [SerializeField] private float speed = 0.2f;
    
    private void Start()
    {
        originalScale = transform.localScale;
        scale.x = originalScale.x * scaleMultiplier.x;
        scale.y = originalScale.y * scaleMultiplier.y;
        scale.z = originalScale.z * scaleMultiplier.z;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.DOScale(scale, speed);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOScale(scale, speed);
    }
}
