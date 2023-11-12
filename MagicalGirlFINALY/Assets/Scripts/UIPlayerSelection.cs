using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerSelection : MonoBehaviour
{
    [field: SerializeField] public Image Image { get; private set; }
    [field: SerializeField] public TextMeshProUGUI NameText { get; private set; }
    [field: SerializeField] public TextMeshProUGUI Text { get; private set; }

    [SerializeField] private Transform colorsLayout;
    [SerializeField] private Image imagePrefab;
    private Dictionary<Color,Image> images = new ();

    public void SetupColors(Color[] colors,GameManager.PlayerData data)
    {
        foreach (var color in colors)
        {
            var img = Instantiate(imagePrefab, colorsLayout);
            img.color = color;
            images.Add(color,img);
        }
        imagePrefab.gameObject.SetActive(false);
        
        data.OnColorChanged += UpdateAvailableColors;
    }

    private void UpdateAvailableColors(List<Color> availableColors,Color selectedColor)
    {
        Color col;
        foreach (var (color, image) in images)
        {
            col = images[selectedColor].color;
            col.a = 1f;
            images[selectedColor].color = col;
            image.gameObject.SetActive(availableColors.Contains(color));
        }

        col = images[selectedColor].color;
        col.a = 0.5f;
        images[selectedColor].color = col;
        images[selectedColor].gameObject.SetActive(true);
    }
}
