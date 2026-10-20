using UnityEngine;

/// <summary>
/// Game 씬 Background(SpriteRenderer)에 붙인다.
/// 화면 비율이 달라도(폴드폰 펼침, PC 가로 화면 등) 배경이 화면을 빈틈없이 덮도록 크기를 맞춘다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundFitter : MonoBehaviour
{
    [Tooltip("카메라 흔들림에도 가장자리가 보이지 않도록 여유분")]
    public float margin = 1.05f;

    SpriteRenderer sr;
    int lastWidth, lastHeight;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    void LateUpdate()
    {
        if (Screen.width == lastWidth && Screen.height == lastHeight) return;
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        Fit();
    }

    void Fit()
    {
        if (sr.sprite == null) return;
        Vector2 size = sr.sprite.bounds.size;
        float viewW = ScreenBounds.Width;
        float viewH = ScreenBounds.Top - ScreenBounds.Bottom;
        if (size.x <= 0f || size.y <= 0f || viewW <= 0f || viewH <= 0f) return;

        float s = Mathf.Max(viewW / size.x, viewH / size.y) * margin;
        transform.localScale = new Vector3(s, s, 1f);
    }
}
