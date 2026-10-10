using UnityEngine;

/// <summary>
/// 코스믹 고래 계열 블랙홀: 앞으로 날아가 멈춘 뒤
/// 주변 적을 빨아들이며 지속 피해 + 범위 안의 적탄 소거.
/// </summary>
public class BlackHole : MonoBehaviour
{
    public Transform visual;
    public SpriteRenderer visualRenderer;
    public float baseRadius = 1.6f;
    public float pullSpeed = 4f;
    public float lifeTime = 2.2f;
    public float travelTime = 0.6f;
    public float tickInterval = 0.15f;
    public float rotateSpeed = -360f;

    float damage, speed, scale, t, tickTimer;
    Vector2 dir;

    public void Init(float damage, Vector2 dir, float speed, float scale, Color color)
    {
        this.damage = damage;
        this.dir = dir.normalized;
        this.speed = speed;
        this.scale = scale;
        if (visualRenderer != null) visualRenderer.color = color;
        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        t += dt;

        // 이동: 점점 감속하다가 정지
        float moveK = 1f - Mathf.Clamp01(t / travelTime);
        transform.position += (Vector3)(dir * speed * moveK * dt);

        // 크기: 커졌다가 마지막에 수축
        float grow = Mathf.Clamp01(t / 0.3f) * Mathf.Clamp01((lifeTime - t) / 0.3f);
        transform.localScale = Vector3.one * scale * grow;
        if (visual != null) visual.Rotate(0f, 0f, rotateSpeed * dt);

        float radius = baseRadius * scale * grow;
        bool tick = false;
        tickTimer -= dt;
        if (tickTimer <= 0f) { tickTimer = tickInterval; tick = true; }

        Vector2 center = transform.position;
        foreach (var col in Physics2D.OverlapCircleAll(center, radius))
        {
            var enemy = col.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                Vector2 to = center - (Vector2)enemy.transform.position;
                if (to.sqrMagnitude > 0.01f) enemy.Pull(to.normalized * pullSpeed * dt);
                if (tick) enemy.TakeDamage(damage);
                continue;
            }

            var bullet = col.GetComponent<EnemyBullet>();
            if (bullet != null) Destroy(bullet.gameObject);
        }

        if (t >= lifeTime) Destroy(gameObject);
    }
}
