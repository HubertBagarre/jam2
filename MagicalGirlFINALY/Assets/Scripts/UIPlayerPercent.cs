using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerPercent : MonoBehaviour
{
    [field: SerializeField] public TextMeshProUGUI PercentText { get; private set; }
    [field: SerializeField] public Image Image { get; private set; }
    
}
