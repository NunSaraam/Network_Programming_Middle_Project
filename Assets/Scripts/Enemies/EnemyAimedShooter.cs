using UnityEngine;

/// <summary>몬스터 2: 화면 중단에 정지 → 플레이어 조준탄 연사 → 퇴장.</summary>
public class EnemyAimedShooter : Enemy
{
    public EnemyBullet bulletPrefab;

    [Header("이동")]
    public float enterSpeed = 3.5f;
    public float exitSpeed = 2.5f;
    [Tooltip("정지할 높이 (화면 비율 0=아래, 1=위)")]
    public Vector2 stopViewportY = new Vector2(0.55f, 0.8f);

    [Header("공격")]
    public float aimDelay = 0.5f;
    public int burstCount = 3;
    public float burstInterval = 0.5f;
    public float bulletSpeed = 4.5f;

    enum State { Enter, Attack, Exit }
    State state;
    float stopY, timer;
    int shotsLeft;

    void Reset()
    {
        baseHp = 45f; goldMin = 12; goldMax = 25; coinCountMin = 2; coinCountMax = 3;
        contactHurtsPlayer = true; dieOnContact = false;
    }

    void Start()
    {
        stopY = ScreenBounds.ViewportY(Random.Range(stopViewportY.x, stopViewportY.y));
    }

    protected override void Move(float dt)
    {
        switch (state)
        {
            case State.Enter:
                transform.position += Vector3.down * enterSpeed * dt;
                if (transform.position.y <= stopY)
                {
                    state = State.Attack;
                    timer = aimDelay;
                    shotsLeft = burstCount + (difficulty >= 3f ? 1 : 0);
                }
                break;

            case State.Attack:
                timer -= dt;
                if (timer <= 0f)
                {
                    FireAimed();
                    timer = burstInterval;
                    if (--shotsLeft <= 0) state = State.Exit;
                }
                break;

            case State.Exit:
                transform.position += Vector3.down * exitSpeed * dt;
                break;
        }
    }

    void FireAimed()
    {
        if (bulletPrefab == null || Player == null || Player.IsDead) return;
        Vector2 dir = Player.transform.position - transform.position;
        var b = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        b.Init(dir, bulletSpeed * BulletSpeedMul);
    }
}
