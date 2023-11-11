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
    
    public void SetupStocks(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            var obj = Instantiate(stockObj,stockLayout);
            stocksObjs.Add(obj);
        }
        stockObj.SetActive(false);
    }

    public void LoseStock()
    {
        if(stocksObjs.Count == 0) return;
        var stock = stocksObjs[0];
        stock.SetActive(false);
        stocksObjs.Remove(stock);
    }
}
