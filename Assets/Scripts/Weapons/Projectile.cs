using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 투사체 (단발/평행/부채꼴/방사/관통 회전/거대 파편 공용).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Tooltip("자식 스프라이트 (회전 연출용)")]
    public SpriteRenderer sr;
    public float lifeTime = 4f;

    float damage, speed, spin, life;
    Vector2 dir;
    int pierceLeft, split;
    readonly HashSet<Enemy> hitEnemies = new HashSet<Enemy>();

    void Awake()
    {
        PhysicsUtil.SetupTriggerBody(gameObject, false);
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
    }

    public void Init(float damage, Vector2 dir, float speed, float scale,
                     int pierce, float spin, int split, Color color)
    {
        this.damage = damage;
        this.dir = dir.normalized;
        this.speed = speed;
        this.pierceLeft = pierce;
        this.spin = spin;
        this.split = split;
        transform.localScale = Vector3.one * scale;
        // 바다 배경 위에서 잘 보이도록 단계 색을 밝게 (흰색 쪽으로 35%)
        if (sr != null) sr.color = Color.Lerp(color, Color.white, 0.35f);
        life = lifeTime;
        hitEnemies.Clear();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        transform.position += (Vector3)(dir * speed * dt);
        if (spin != 0f && sr != null) sr.transform.Rotate(0f, 0f, spin * dt);

        life -= dt;
        if (life <= 0f || ScreenBounds.IsOutside(transform.position, 1f))
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 암석 장벽은 일반탄을 막는다 (관통 톱날은 통과)
        if (other.GetComponentInParent<Barrier>() != null)
        {
            if (pierceLeft < 100) Destroy(gameObject);
            return;
        }

        var enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null || !hitEnemies.Add(enemy)) return;

        enemy.TakeDamage(damage);

        if (split > 0)
        {
            Split(enemy);
            Destroy(gameObject);
            return;
        }

        if (pierceLeft-- <= 0) Destroy(gameObject);
    }

    /// <summary>거대 파편: 명중 지점에서 사방으로 작은 파편 분열</summary>
    void Split(Enemy source)
    {
        float start = Random.Range(0f, 360f);
        Color color = sr != null ? sr.color : Color.white;

        for (int i = 0; i < split; i++)
        {
            float a = start + 360f / split * i;
            Vector2 d = Quaternion.Euler(0f, 0f, a) * Vector2.up;
            var frag = Instantiate(this, transform.position, Quaternion.Euler(0f, 0f, a));
            frag.Init(damage * 0.35f, d, speed * 1.5f, transform.localScale.x * 0.35f, 0, 0f, 0, color);
            frag.life = 0.6f;
            frag.hitEnemies.Add(source); // 맞은 적에게 중복 피해 X
        }
    }
}
