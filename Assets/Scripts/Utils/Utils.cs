using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카메라 기준 화면 경계(월드 좌표). 프레임당 한 번만 계산해서 캐싱한다.
/// </summary>
public static class ScreenBounds
{
    static int cachedFrame = -1;
    static float left, right, top, bottom;

    static void Ensure()
    {
        if (cachedFrame == Time.frameCount) return;
        var cam = Camera.main;
        if (cam == null) return;
        cachedFrame = Time.frameCount;
        // z = 카메라에서 게임 평면(z=0)까지의 거리 → Orthographic/Perspective 둘 다 올바른 경계
        float dist = Mathf.Abs(cam.transform.position.z);
        if (dist < 0.01f) dist = 10f;
        Vector3 min = cam.ViewportToWorldPoint(new Vector3(0f, 0f, dist));
        Vector3 max = cam.ViewportToWorldPoint(new Vector3(1f, 1f, dist));
        left = Mathf.Min(min.x, max.x); right = Mathf.Max(min.x, max.x);
        bottom = Mathf.Min(min.y, max.y); top = Mathf.Max(min.y, max.y);
    }

    public static float Left   { get { Ensure(); return left; } }
    public static float Right  { get { Ensure(); return right; } }
    public static float Top    { get { Ensure(); return top; } }
    public static float Bottom { get { Ensure(); return bottom; } }
    public static float Width => Right - Left;

    /// <summary>뷰포트 비율(0=아래, 1=위)을 월드 Y로 변환</summary>
    public static float ViewportY(float v) => Mathf.Lerp(Bottom, Top, v);

    public static bool IsOutside(Vector3 p, float margin)
    {
        return p.x < Left - margin || p.x > Right + margin ||
               p.y < Bottom - margin || p.y > Top + margin;
    }
}

/// <summary>
/// 2D 트리거 충돌 세팅 규칙
///  - 받는 쪽(플레이어 / 적 / 장벽) = Dynamic (중력 0)
///  - 공격체(플레이어탄 / 빔 / 적탄)  = Kinematic
/// Kinematic끼리는 트리거가 발생하지 않으므로 이 규칙을 지켜야 충돌이 확실히 잡힌다.
/// </summary>
public static class PhysicsUtil
{
    public static Rigidbody2D SetupTriggerBody(GameObject go, bool dynamic)
    {
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb == null) rb = go.AddComponent<Rigidbody2D>();

        rb.bodyType = dynamic ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep; // 멈춰 있는 적도 OnTriggerStay를 받도록

        foreach (var c in go.GetComponentsInChildren<Collider2D>())
            c.isTrigger = true;

        return rb;
    }
}

/// <summary>
/// Resources/Fish/fish_01 ~ fish_20 스프라이트를 불러온다.
/// 스프라이트가 없으면 테이블의 색상으로 대체(프로토타입용).
/// </summary>
public static class FishVisuals
{
    public static Sprite Load(int stage) => Resources.Load<Sprite>($"Fish/fish_{stage:00}");

    public static void Apply(SpriteRenderer sr, int stage)
    {
        if (sr == null) return;
        var sprite = Load(stage);
        if (sprite != null) { sr.sprite = sprite; sr.color = Color.white; }
        else sr.color = FishEvolutionTable.Get(stage).Tint;
    }

    public static void Apply(Image img, int stage)
    {
        if (img == null) return;
        var sprite = Load(stage);
        if (sprite != null) { img.sprite = sprite; img.color = Color.white; img.preserveAspect = true; }
        else img.color = FishEvolutionTable.Get(stage).Tint;
    }
}
