using UnityEngine;

/// <summary>적 탄환. 잠수 중인 플레이어는 통과한다.</summary>
[RequireComponent(typeof(Collider2D))]
public class EnemyBullet : MonoBehaviour
{
    public float lifeTime = 8f;

    Vector2 velocity;
    float life;

    void Awake()
    {
        PhysicsUtil.SetupTriggerBody(gameObject, false);
    }

    public void Init(Vector2 dir, float speed)
    {
        velocity = dir.normalized * speed;
        transform.up = velocity.normalized;
        life = lifeTime;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        transform.position += (Vector3)(velocity * dt);
        life -= dt;
        if (life <= 0f || ScreenBounds.IsOutside(transform.position, 0.5f))
            Destroy(gameObject);
    }

    // Enter가 아니라 Stay: 잠수가 끝나는 순간 겹쳐 있던 탄도 맞도록
    void OnTriggerStay2D(Collider2D other)
    {
        var rb = other.attachedRigidbody;
        if (rb == null || !rb.TryGetComponent(out PlayerController player)) return;
        if (player.Hit()) Destroy(gameObject);
    }
}
