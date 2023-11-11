using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    Vector3 originalScale;
    [SerializeField] private Vector3 scaleMultiplier = new Vector3(1.2f,1.2f,1);
    [SerializeField] private float speed = 0.2f;
    
    private void Start()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.DOScale(scaleMultiplier, speed);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOScale(originalScale, speed);
    }
}
