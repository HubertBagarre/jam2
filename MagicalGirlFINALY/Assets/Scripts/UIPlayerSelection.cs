using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerSelection : MonoBehaviour
{
    [field: SerializeField] public Image Image { get; private set; }
    [field: SerializeField] public TextMeshProUGUI NameText { get; private set; }
    [field: SerializeField] public TextMeshProUGUI Text { get; private set; }
    
    [SerializeField] private Image imagePrefab;
    
    public void ChangeColor(List<Color> availableColors,Color selectedColor)
    {
        imagePrefab.color = selectedColor;
    }
}
