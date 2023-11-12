using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI fpsCountText;
    [SerializeField] private TextMeshProUGUI debugText;
    
    public List<int> frames = new ();
    
    // Start is called before the first frame update
    private void Start()
    {
        for (int i = 0; i < 100; i++)
        {
            frames.Add(0);
        }
    }
    
    private void Update()
    {
        frames.RemoveAt(0);
        frames.Add((int)(1f / Time.unscaledDeltaTime));
        
        fpsCountText.text = $"FPS: {frames.Average()}";
    }
}
