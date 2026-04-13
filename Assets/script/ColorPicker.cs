using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ColorPicker : MonoBehaviour
{
    [Header("UI Elements")]
    public RawImage hueWheel;           // 色相ホイール
    public RawImage svBox;              // 彩度・明度ボックス
    public Image colorPreview;          // 色プレビュー
    public Image hueIndicator;          // 色相の現在位置
    public Image svIndicator;           // SV の現在位置
    
    [Header("Current Color")]
    public Color currentColor = Color.white;
    
    private float hue = 0f;             // 0-1
    private float saturation = 1f;      // 0-1
    private float value = 1f;           // 0-1
    
    private Texture2D hueWheelTexture;
    private Texture2D svBoxTexture;
    
    private bool isDraggingHue = false;
    private bool isDraggingSV = false;
    
    public System.Action<Color> onColorChanged;
    
    void Start()
    {
        Debug.Log("===== ColorPicker Start =====");
        Debug.Log($"HueWheel: {(hueWheel != null ? "OK" : "NULL")}");
        Debug.Log($"SVBox: {(svBox != null ? "OK" : "NULL")}");
        Debug.Log($"ColorPreview: {(colorPreview != null ? "OK" : "NULL")}");
        Debug.Log($"HueIndicator: {(hueIndicator != null ? "OK" : "NULL")}");
        Debug.Log($"SVIndicator: {(svIndicator != null ? "OK" : "NULL")}");
        
        CreateHueWheel();
        CreateSVBox();
        UpdatePreview();
        
        // タッチイベント設定
        AddEventTrigger(hueWheel.gameObject, EventTriggerType.PointerDown, OnHuePointerDown);
        AddEventTrigger(hueWheel.gameObject, EventTriggerType.Drag, OnHueDrag);
        AddEventTrigger(hueWheel.gameObject, EventTriggerType.PointerUp, OnHuePointerUp);
        
        AddEventTrigger(svBox.gameObject, EventTriggerType.PointerDown, OnSVPointerDown);
        AddEventTrigger(svBox.gameObject, EventTriggerType.Drag, OnSVDrag);
        AddEventTrigger(svBox.gameObject, EventTriggerType.PointerUp, OnSVPointerUp);
    }
    
    // ===== 色相ホイール生成 =====
    void CreateHueWheel()
    {
        int size = 256;
        hueWheelTexture = new Texture2D(size, size);
        
        Vector2 center = new Vector2(size / 2, size / 2);
        float outerRadius = size / 2;
        float innerRadius = size / 2 * 0.6f; // ドーナツ型
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                
                if (distance > outerRadius || distance < innerRadius)
                {
                    // 外側と内側は透明
                    hueWheelTexture.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
                else
                {
                    // 角度から色相を計算
                    float angle = Mathf.Atan2(y - center.y, x - center.x);
                    float hueValue = (angle / (2 * Mathf.PI)) + 0.5f;
                    if (hueValue < 0) hueValue += 1f;
                    
                    Color color = Color.HSVToRGB(hueValue, 1f, 1f);
                    hueWheelTexture.SetPixel(x, y, color);
                }
            }
        }
        
        hueWheelTexture.Apply();
        hueWheel.texture = hueWheelTexture;
    }
    
    // ===== SV ボックス生成 =====
    void CreateSVBox()
    {
        int size = 256;
        svBoxTexture = new Texture2D(size, size);
        UpdateSVBox();
        svBox.texture = svBoxTexture;
    }
    
    void UpdateSVBox()
    {
        int size = svBoxTexture.width;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float s = (float)x / size; // 横軸: 彩度
                float v = (float)y / size; // 縦軸: 明度
                
                Color color = Color.HSVToRGB(hue, s, v);
                svBoxTexture.SetPixel(x, y, color);
            }
        }
        
        svBoxTexture.Apply();
    }
    
    // ===== 色相ホイール タッチ =====
    void OnHuePointerDown(BaseEventData data)
    {
        isDraggingHue = true;
        OnHueDrag(data);
    }
    
    void OnHueDrag(BaseEventData data)
    {
        if (!isDraggingHue) return;
        
        PointerEventData pointerData = data as PointerEventData;
        if (pointerData == null) return;
        
        RectTransform rect = hueWheel.rectTransform;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, pointerData.position, pointerData.pressEventCamera, out localPoint);
        
        // 中心からの角度を計算
        float angle = Mathf.Atan2(localPoint.y, localPoint.x);
        hue = (angle / (2 * Mathf.PI)) + 0.5f;
        if (hue < 0) hue += 1f;
        if (hue > 1) hue -= 1f;
        
        UpdateSVBox();
        UpdatePreview();
        UpdateIndicators();
        
        // ⭐ デバッグログ追加
        Debug.Log($"OnHueDrag: onColorChanged is {(onColorChanged != null ? "NOT NULL" : "NULL")}");
        
        if (onColorChanged != null)
        {
            Debug.Log($"OnHueDrag: Invoking onColorChanged with color R={currentColor.r}, G={currentColor.g}, B={currentColor.b}");
            onColorChanged(currentColor);
        }
    }
    
    void OnHuePointerUp(BaseEventData data)
    {
        isDraggingHue = false;
    }
    
    // ===== SV ボックス タッチ =====
    void OnSVPointerDown(BaseEventData data)
    {
        isDraggingSV = true;
        OnSVDrag(data);
    }
    
    void OnSVDrag(BaseEventData data)
    {
        if (!isDraggingSV) return;
        
        PointerEventData pointerData = data as PointerEventData;
        if (pointerData == null) return;
        
        RectTransform rect = svBox.rectTransform;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, pointerData.position, pointerData.pressEventCamera, out localPoint);
        
        // 0-1 の範囲に正規化
        saturation = Mathf.Clamp01((localPoint.x + rect.rect.width / 2) / rect.rect.width);
        value = Mathf.Clamp01((localPoint.y + rect.rect.height / 2) / rect.rect.height);
        
        UpdatePreview();
        UpdateIndicators();
        
        // ⭐ デバッグログ追加
        Debug.Log($"OnSVDrag: onColorChanged is {(onColorChanged != null ? "NOT NULL" : "NULL")}");
        
        if (onColorChanged != null)
        {
            Debug.Log($"OnSVDrag: Invoking onColorChanged with color R={currentColor.r}, G={currentColor.g}, B={currentColor.b}");
            onColorChanged(currentColor);
        }
    }
    
    void OnSVPointerUp(BaseEventData data)
    {
        isDraggingSV = false;
    }
    
    // ===== プレビュー更新 =====
    void UpdatePreview()
    {
        currentColor = Color.HSVToRGB(hue, saturation, value);
        
        Debug.Log($"ColorPicker: Color changed to R={currentColor.r}, G={currentColor.g}, B={currentColor.b}");
        
        if (colorPreview != null)
        {
            colorPreview.color = currentColor;
        }
        else
        {
            Debug.LogError("ColorPicker: colorPreview is NULL!");
        }
    }
    
    // ===== インジケーター更新 =====
    void UpdateIndicators()
    {
        // 色相インジケーター
        if (hueIndicator != null)
        {
            float angle = (hue - 0.5f) * 2 * Mathf.PI;
            float radius = hueWheel.rectTransform.rect.width / 2 * 0.8f;
            
            Vector2 pos = new Vector2(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius
            );
            
            hueIndicator.rectTransform.anchoredPosition = pos;
        }
        
        // SV インジケーター
        if (svIndicator != null)
        {
            RectTransform rect = svBox.rectTransform;
            
            Vector2 pos = new Vector2(
                (saturation - 0.5f) * rect.rect.width,
                (value - 0.5f) * rect.rect.height
            );
            
            svIndicator.rectTransform.anchoredPosition = pos;
        }
    }
    
    // ===== 外部から色を設定 =====
    public void SetColor(Color color)
    {
        Color.RGBToHSV(color, out hue, out saturation, out value);
        
        UpdateSVBox();
        UpdatePreview();
        UpdateIndicators();
    }
    
    // ===== EventTrigger 追加ヘルパー =====
    void AddEventTrigger(GameObject target, EventTriggerType eventType, System.Action<BaseEventData> callback)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = target.AddComponent<EventTrigger>();
        }
        
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = eventType;
        entry.callback.AddListener((data) => callback(data));
        trigger.triggers.Add(entry);
    }
}