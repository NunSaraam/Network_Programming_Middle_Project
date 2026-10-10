using UnityEngine;

/// <summary>몬스터 3: 상단에 머물며 좌우로 흔들리면서 아래로 부채꼴 탄막 발사.</summary>
public class EnemyFanShooter : Enemy
{
    public EnemyBullet bulletPrefab;

    [Header("이동")]
    public float enterSpeed = 2.5f;
    public float exitSpeed = 2f;
    public Vector2 stopViewportY = new Vector2(0.65f, 0.85f);
    public float swayAmplitude = 1.2f;
    public float swayFrequency = 0.4f;
    public float stayTime = 7f;

    [Header("공격")]
    public int bulletCount = 5;
    public float fanAngle = 60f;
    public float fireInterval = 1.6f;
    public float bulletSpeed = 3.5f;

    enum State { Enter, Attack, Exit }
    State state;
    float stopY, anchorX, timer, stayTimer, swayT;
    bool altOffset;

    void Reset()
    {
        baseHp = 80f; goldMin = 25; goldMax = 45; coinCountMin = 3; coinCountMax = 5;
        contactHurtsPlayer = true; dieOnContact = false; deathFxScale = 1.5f;
    }

    void Start()
    {
        stopY = ScreenBounds.ViewportY(Random.Range(stopViewportY.x, stopViewportY.y));
        float minX = ScreenBounds.Left + swayAmplitude + 0.5f;
        float maxX = ScreenBounds.Right - swayAmplitude - 0.5f;
        anchorX = minX < maxX ? Mathf.Clamp(transform.position.x, minX, maxX) : 0f;
    }

    protected override void Move(float dt)
    {
        Vector3 p = transform.position;

        switch (state)
        {
            case State.Enter:
                p.y -= enterSpeed * dt;
                p.x = Mathf.MoveTowards(p.x, anchorX, enterSpeed * dt);
                if (p.y <= stopY)
                {
                    state = State.Attack;
                    timer = 0.4f;
                    stayTimer = stayTime;
                }
                break;

            case State.Attack:
                swayT += dt;
                p.x = anchorX + Mathf.Sin(swayT * swayFrequency * Mathf.PI * 2f) * swayAmplitude;

                timer -= dt;
                if (timer <= 0f) { FireFan(); timer = fireInterval; }

                stayTimer -= dt;
                if (stayTimer <= 0f) state = State.Exit;
                break;

            case State.Exit:
                p.y -= exitSpeed * dt;
                break;
        }

        transform.position = p;
    }

    void FireFan()
    {
        if (bulletPrefab == null) return;

        int count = bulletCount + (difficulty >= 3f ? 2 : 0);
        float step = count > 1 ? fanAngle / (count - 1) : 0f;
        float offset = altOffset ? step * 0.5f : 0f; // 번갈아 반 칸씩 틀어서 빈틈 위치를 바꿈
        altOffset = !altOffset;

        for (int i = 0; i < count; i++)
        {
            float angle = 180f - fanAngle * 0.5f + step * i + offset; // 180도 = 아래
            Vector2 dir = Quaternion.Euler(0f, 0f, angle) * Vector2.up;
            var b = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            b.Init(dir, bulletSpeed * BulletSpeedMul);
        }
    }
}
