using UnityEngine;

/// <summary>
/// 몬스터 4: 난입하는 황금 보물 해마.
/// 화면 옆에서 들어와 출렁이며 가로지른다. 공격하지 않으며, 놓치면 그대로 도망간다.
/// </summary>
public class GoldenSeahorse : Enemy
{
    public float speed = 1.8f;
    public float bobAmplitude = 0.5f;
    public float bobFrequency = 1.5f;
    public float sinkSpeed = 0.15f;

    [Header("반짝임")]
    public Color glowA = new Color(1f, 0.82f, 0.2f);
    public Color glowB = Color.white;
    public float glowSpeed = 6f;

    float side, baseY, t;

    protected override Color IdleColor =>
        Color.Lerp(glowA, glowB, (Mathf.Sin(Time.time * glowSpeed) + 1f) * 0.25f);

    void Reset()
    {
        baseHp = 150f; goldMin = 200; goldMax = 400; coinCountMin = 12; coinCountMax = 18;
        contactHurtsPlayer = false; dieOnContact = false; deathFxScale = 2f;
    }

    void Start()
    {
        side = transform.position.x < 0f ? 1f : -1f; // 왼쪽에서 나오면 오른쪽으로
        baseY = transform.position.y;
        if (sr != null) sr.flipX = side < 0f;
    }

    protected override void Move(float dt)
    {
        t += dt;
        Vector3 p = transform.position;
        p.x += side * speed * dt;
        p.y = baseY - sinkSpeed * t + Mathf.Sin(t * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        transform.position = p;
    }
}
