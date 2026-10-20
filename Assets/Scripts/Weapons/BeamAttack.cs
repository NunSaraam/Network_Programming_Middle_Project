using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레이저(고정 방향) / 촉수 채찍(휘두르기) 공용 빔.
/// 구조: 루트(BeamAttack) └ Body (SpriteRenderer)
/// 루트 피벗이 빔의 시작점이고, 그림(Body)과 판정(루트의 BoxCollider2D)을 코드가 같은 크기로 맞춘다.
/// 스프라이트의 Pixels Per Unit이나 프리팹에 붙은 콜라이더 위치와 상관없이 동작한다.
/// </summary>
public class BeamAttack : MonoBehaviour
{
    public Transform body;
    public SpriteRenderer bodyRenderer;
    public float tickInterval = 0.1f;

    [Tooltip("판정 굵기 = 보이는 굵기 × 이 값")]
    public float hitWidthRatio = 1f;
    [Tooltip("빔 색을 흰색 쪽으로 밝히는 정도 (배경 위에서 잘 보이게)")]
    [Range(0f, 1f)] public float whiten = 0.35f;
    [Tooltip("에너지가 흐르는 느낌의 굵기 떨림")]
    public float flicker = 0.12f;

    Transform follow;
    BoxCollider2D hitbox;
    Vector2 spriteSize = Vector2.one;
    float damage, startAngle, sweep, length, width, duration, t;
    readonly Dictionary<Enemy, float> nextHitTime = new Dictionary<Enemy, float>();

    void Awake()
    {
        PhysicsUtil.SetupTriggerBody(gameObject, false);
        if (bodyRenderer == null && body != null) bodyRenderer = body.GetComponent<SpriteRenderer>();
        if (body == null && bodyRenderer != null) body = bodyRenderer.transform;

        // 스프라이트 실제 크기(PPU에 따라 다름)로 나눠서, 그림이 정확히 width × length가 되게 함
        if (bodyRenderer != null && bodyRenderer.sprite != null)
            spriteSize = bodyRenderer.sprite.bounds.size;

        // 판정은 루트의 BoxCollider2D 하나로 통일 (자식에 붙은 콜라이더는 제거)
        foreach (var c in GetComponentsInChildren<Collider2D>())
            if (c.gameObject != gameObject) Destroy(c);
        hitbox = GetComponent<BoxCollider2D>();
        if (hitbox == null) hitbox = gameObject.AddComponent<BoxCollider2D>();
        hitbox.isTrigger = true;

        // 빔이 물고기를 가리지 않도록 물고기보다 뒤에 그림
        if (bodyRenderer != null) bodyRenderer.sortingOrder = -1;
    }

    public void Init(Transform follow, float damage, float startAngle, float sweep,
                     float length, float width, float duration, Color color)
    {
        this.follow = follow;
        this.damage = damage;
        this.startAngle = startAngle;
        this.sweep = sweep;
        this.length = length;
        this.width = width;
        this.duration = Mathf.Max(0.05f, duration);
        if (bodyRenderer != null) bodyRenderer.color = Color.Lerp(color, Color.white, whiten);
        Apply(0f);
    }

    void LateUpdate()
    {
        t += Time.deltaTime;
        float n = Mathf.Clamp01(t / duration);
        Apply(n);
        if (t >= duration) Destroy(gameObject);
    }

    void Apply(float n)
    {
        if (follow != null) transform.position = follow.position;

        float eased = n * n * (3f - 2f * n); // smoothstep
        transform.rotation = Quaternion.Euler(0f, 0f, startAngle + sweep * eased);

        // 굵기: 빠르게 펼쳐졌다가 끝에서 가늘어짐 + 살짝 떨림
        float envelope = Mathf.Clamp01(Mathf.Min(n / 0.1f, (1f - n) / 0.2f));
        float w = width * envelope * (1f + flicker * Mathf.Sin(Time.time * 45f));

        if (body != null)
        {
            body.localScale = new Vector3(Mathf.Max(0.02f, w) / spriteSize.x, length / spriteSize.y, 1f);
            body.localPosition = new Vector3(0f, length * 0.5f, 0f);
        }

        if (hitbox != null)
        {
            // 펼쳐진 동안은 보이는 굵기 그대로, 너무 가늘 때도 최소 판정은 유지
            hitbox.size = new Vector2(Mathf.Max(0.15f, width * envelope * hitWidthRatio), length);
            hitbox.offset = new Vector2(0f, length * 0.5f);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        var enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null) return;

        if (nextHitTime.TryGetValue(enemy, out float next) && Time.time < next) return;
        nextHitTime[enemy] = Time.time + tickInterval;
        enemy.TakeDamage(damage);
    }
}
