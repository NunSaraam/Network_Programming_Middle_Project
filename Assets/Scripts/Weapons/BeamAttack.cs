using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레이저(고정 방향) / 촉수 채찍(휘두르기) 공용 빔.
/// 구조: 루트(BeamAttack) └ Body (SpriteRenderer + BoxCollider2D, 1x1 크기 스프라이트)
/// 루트 피벗이 빔의 시작점이고 Body가 위쪽으로 length만큼 늘어난다.
/// </summary>
public class BeamAttack : MonoBehaviour
{
    public Transform body;
    public SpriteRenderer bodyRenderer;
    public float tickInterval = 0.1f;

    Transform follow;
    float damage, startAngle, sweep, length, width, duration, t;
    readonly Dictionary<Enemy, float> nextHitTime = new Dictionary<Enemy, float>();

    void Awake()
    {
        PhysicsUtil.SetupTriggerBody(gameObject, false);
        if (bodyRenderer == null && body != null) bodyRenderer = body.GetComponent<SpriteRenderer>();
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
        if (bodyRenderer != null) bodyRenderer.color = color;
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

        // 굵기: 빠르게 펼쳐졌다가 끝에서 가늘어짐
        float w = width * Mathf.Clamp01(Mathf.Min(n / 0.12f, (1f - n) / 0.25f));
        if (body != null)
        {
            body.localScale = new Vector3(Mathf.Max(0.02f, w), length, 1f);
            body.localPosition = new Vector3(0f, length * 0.5f, 0f);
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
