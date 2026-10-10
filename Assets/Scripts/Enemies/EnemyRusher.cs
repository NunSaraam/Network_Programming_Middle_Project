using UnityEngine;

/// <summary>몬스터 1: 단순 돌진형 - 등장 순간 플레이어 쪽으로 살짝 조준해서 직진.</summary>
public class EnemyRusher : Enemy
{
    public float speed = 5f;
    [Range(0f, 1f)] public float aimStrength = 0.35f;

    Vector2 dir = Vector2.down;

    void Reset()
    {
        baseHp = 20f; goldMin = 5; goldMax = 12; coinCountMin = 1; coinCountMax = 2;
        contactHurtsPlayer = true; dieOnContact = true;
    }

    void Start()
    {
        if (Player != null)
        {
            Vector2 toPlayer = ((Vector2)(Player.transform.position - transform.position)).normalized;
            dir = Vector2.Lerp(Vector2.down, toPlayer, aimStrength).normalized;
        }
        // 스프라이트가 아래를 보고 있다고 가정
        transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.down, dir));
    }

    protected override void Move(float dt)
    {
        transform.position += (Vector3)(dir * speed * dt);
    }
}
