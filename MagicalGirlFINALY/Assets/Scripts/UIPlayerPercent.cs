using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerPercent : MonoBehaviour
{
    [field: SerializeField] public TextMeshProUGUI PercentText { get; private set; }
    [field: SerializeField] public Image Image { get; private set; }
    [SerializeField] private GameObject stockObj;
    [SerializeField] private Transform stockLayout;
    public List<GameObject> stocksObjs = new List<GameObject>();
    [SerializeField] private Image transformationChargeImage;
    
    public void Setup(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            var obj = Instantiate(stockObj,stockLayout);
            stocksObjs.Add(obj);
        }
        stockObj.SetActive(false);
    }
    
    public void UpdatePercent(int previous,int percent)
    {
        PercentText.text = $"{percent}%"; //TODO anim stylé
    }

    public void LoseStock()
    {
        if(stocksObjs.Count == 0) return;
        var stock = stocksObjs[0];
        stock.SetActive(false);
        stocksObjs.Remove(stock);
    }

    public void UpdateTransformationCharge(float value)
    {
        transformationChargeImage.fillAmount = value;
        
        //TODO dotween smooth
    }
}
