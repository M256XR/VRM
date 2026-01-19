using UnityEngine;
using UnityEngine.UI;

public class WallpaperTouchTest : MonoBehaviour
{
    public Text statusText;
    private int tapCount = 0;
    
    void Start()
    {
        Debug.Log("========== WallpaperTouchTest Start called! ==========");
        
        // いったん全部コメントアウト
        
        if (statusText != null)
        {
            statusText.text = "壁紙起動！タップしてね";
            statusText.fontSize = 20;
            Debug.Log("Text updated!");
        }
        else
        {
            Debug.Log("ERROR: statusText is null!");
        }
        
    }
    
    // Update もいったんコメントアウト
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            tapCount++;
            UpdateText();
        }
    }
    
    void UpdateText()
    {
        if (statusText != null)
        {
            statusText.text = $"タップ回数: {tapCount}";
            statusText.color = Random.ColorHSV();
        }
    }
}