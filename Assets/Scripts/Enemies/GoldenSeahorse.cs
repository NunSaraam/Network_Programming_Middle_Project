using UnityEngine;

/// <summary>
/// 몬스터 4: 난입하는 황금 보물 해마.
/// 화면 옆에서 들어와 → 가운데에서 잠시 머물며 출렁이고 → 반대쪽으로 도망간다.
/// 공격하지 않으며, 놓치면 그대로 사라진다.
/// </summary>
public class GoldenSeahorse : Enemy
{
    public float speed = 1.8f;
    public float bobAmplitude = 0.5f;
    public float bobFrequency = 1.5f;
    public float sinkSpeed = 0.15f;

    [Header("머무르기 (잡을 기회)")]
    public float wanderTime = 4f;
    public float wanderRange = 1.1f;
    public float escapeSpeedMul = 1.6f;

    [Header("체력 (다른 몬스터보다 덜 단단해지도록)")]
    [Tooltip("Base Hp에 곱하는 값")]
    public float hpMultiplier = 0.4f;
    [Tooltip("시간에 따른 체력 증가를 일반 몬스터의 몇 배로 할지")]
    public float hpGrowthFactor = 0.3f;

    [Header("반짝임")]
    public Color glowA = new Color(1f, 0.82f, 0.2f);
    public Color glowB = Color.white;
    public float glowSpeed = 6f;

    enum State { Enter, Wander, Escape }
    State state;
    float side, baseY, targetX, t, wanderTimer;

    protected override Color IdleColor =>
        Color.Lerp(glowA, glowB, (Mathf.Sin(Time.time * glowSpeed) + 1f) * 0.25f);

    void Reset()
    {
        baseHp = 150f; goldMin = 200; goldMax = 400; coinCountMin = 12; coinCountMax = 18;
        contactHurtsPlayer = false; dieOnContact = false; deathFxScale = 2f;
    }

    public override void Init(float difficulty, float goldMul, float hpScale = 1f)
    {
        base.Init(difficulty, goldMul, hpScale);
        float growth = 1f + (this.difficulty - 1f) * hpGrowthFactor;
        hp = maxHp = baseHp * hpMultiplier * growth * hpScale;
    }

    void Start()
    {
        side = transform.position.x < 0f ? 1f : -1f; // 왼쪽에서 나오면 오른쪽으로
        baseY = transform.position.y;
        targetX = Mathf.Lerp(ScreenBounds.Left, ScreenBounds.Right, Random.Range(0.35f, 0.65f));
        wanderTimer = wanderTime;
        if (sr != null) sr.flipX = side < 0f;
        if (showHpBar) CreateHpBar(); // 보물이라는 걸 알 수 있게 처음부터 체력바 표시
    }

    protected override void Move(float dt)
    {
        t += dt;
        Vector3 p = transform.position;

        switch (state)
        {
            case State.Enter:
                p.x = Mathf.MoveTowards(p.x, targetX, speed * dt);
                if (Mathf.Abs(p.x - targetX) < 0.01f) state = State.Wander;
                break;

            case State.Wander:
                wanderTimer -= dt;
                float wx = targetX + Mathf.Sin((wanderTime - wanderTimer) * 1.2f) * wanderRange;
                p.x = Mathf.MoveTowards(p.x, wx, speed * dt);
                if (wanderTimer <= 0f) state = State.Escape;
                break;

            case State.Escape:
                p.x += side * speed * escapeSpeedMul * dt;
                break;
        }

        p.y = baseY - sinkSpeed * t + Mathf.Sin(t * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        transform.position = p;
    }
}
