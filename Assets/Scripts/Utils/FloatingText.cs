using TMPro;
using UnityEngine;

/// <summary>
/// 월드 위에 잠깐 떠올랐다 사라지는 글자 ("+12 G", "무적!" 등).
/// 프리팹 없이 FloatingText.Spawn(...) 한 줄로 사용.
/// 폰트는 인게임 HUD 골드 텍스트의 폰트(한글 포함)를 자동으로 가져다 쓴다.
/// </summary>
public class FloatingText : MonoBehaviour
{
    const float LifeTime = 0.8f;

    /// <summary>직접 지정하고 싶으면 이 값을 설정 (null이면 HUD 폰트 자동 사용)</summary>
    public static TMP_FontAsset Font;

    static TMP_FontAsset ResolveFont()
    {
        if (Font != null) return Font;
        var gm = GameManager.Instance;
        if (gm != null && gm.ui != null && gm.ui.goldText != null) Font = gm.ui.goldText.font;
        return Font;
    }

    TextMeshPro tmp;
    Color color;
    float t, baseSize;
    Vector3 velocity;

    public static void Spawn(Vector3 worldPos, string text, Color color, float fontSize = 3.5f)
    {
        var go = new GameObject("FloatingText");
        go.transform.position = worldPos + (Vector3)(Random.insideUnitCircle * 0.15f) + Vector3.back * 0.1f;
        go.AddComponent<FloatingText>().Setup(text, color, fontSize);
    }

    void Setup(string text, Color c, float fontSize)
    {
        tmp = gameObject.AddComponent<TextMeshPro>();
        var font = ResolveFont();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = c;
        tmp.sortingOrder = 200;
        tmp.rectTransform.sizeDelta = new Vector2(4f, 1f);

        color = c;
        baseSize = fontSize;
        velocity = new Vector3(Random.Range(-0.3f, 0.3f), 1.8f, 0f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        t += dt;

        transform.position += velocity * dt;
        velocity *= 1f - 3f * dt; // 위로 튀었다가 감속

        // 크기: 툭 튀어나왔다가 제자리
        float pop = t < 0.1f ? Mathf.Lerp(0.6f, 1.25f, t / 0.1f) : Mathf.Lerp(1.25f, 1f, (t - 0.1f) / 0.15f);
        tmp.fontSize = baseSize * pop;

        // 후반에 서서히 투명하게
        float fade = Mathf.Clamp01((t - LifeTime * 0.5f) / (LifeTime * 0.5f));
        Color c = color; c.a = color.a * (1f - fade);
        tmp.color = c;

        if (t >= LifeTime) Destroy(gameObject);
    }
}
