using UnityEngine;

/// <summary>
/// 드롭 골드 코인. 값에 따라 크기/색이 달라진다 (동/은/금).
/// 튀어 오른 뒤 천천히 가라앉고, 플레이어가 가까이 오면 자석처럼 빨려온다.
/// 물리 충돌 대신 거리 계산으로 획득 → 콜라이더 불필요.
/// </summary>
public class Coin : MonoBehaviour
{
    public SpriteRenderer sr;

    [Header("움직임")]
    public float magnetRadius = 2.2f;
    public float magnetAccel = 30f;
    public float maxMagnetSpeed = 14f;
    public float collectRadius = 0.45f;
    public float gravity = 6f;
    public float maxFallSpeed = 1.8f;
    public float lifeTime = 10f;

    [Header("크기별 등급")]
    public int silverThreshold = 20;
    public int goldThreshold = 80;
    public Color bronzeColor = new Color(0.85f, 0.55f, 0.25f);
    public Color silverColor = new Color(0.85f, 0.9f, 1f);
    public Color goldColor = new Color(1f, 0.85f, 0.2f);

    int value;
    Vector2 velocity;
    bool magnet;
    float life;

    void Awake()
    {
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
    }

    public void Init(int value)
    {
        this.value = value;

        float scale; Color color;
        if (value >= goldThreshold)        { scale = 0.55f; color = goldColor; }
        else if (value >= silverThreshold) { scale = 0.4f;  color = silverColor; }
        else                               { scale = 0.28f; color = bronzeColor; }

        transform.localScale = Vector3.one * scale;
        if (sr != null) sr.color = color;

        velocity = new Vector2(Random.Range(-1.8f, 1.8f), Random.Range(2f, 4.5f));
        life = lifeTime;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        var player = PlayerController.Instance;

        if (player != null && !player.IsDead)
        {
            Vector2 to = player.transform.position - transform.position;
            float dist = to.magnitude;
            if (dist <= collectRadius) { Collect(); return; }
            if (dist <= magnetRadius) magnet = true;

            if (magnet)
            {
                float speed = Mathf.Min(velocity.magnitude + magnetAccel * dt, maxMagnetSpeed);
                velocity = to.normalized * speed;
            }
        }
        else
        {
            magnet = false;
        }

        if (!magnet)
        {
            velocity.y = Mathf.Max(velocity.y - gravity * dt, -maxFallSpeed);
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, 2f * dt);
            life -= dt;
        }

        transform.position += (Vector3)(velocity * dt);

        if (life <= 0f || transform.position.y < ScreenBounds.Bottom - 1f)
            Destroy(gameObject);
    }

    void Collect()
    {
        var gm = GameManager.Instance;
        if (gm != null) gm.AddGold(value);
        SfxPlayer.Play(SfxType.Coin, 0.5f);
        Destroy(gameObject);
    }
}
