using UnityEngine;

/// <summary>
/// 모든 몬스터의 공통 기반: 체력, 피격 연출, 골드 드롭, 몸통 박치기 판정.
/// </summary>
public abstract class Enemy : MonoBehaviour
{
    [Header("기본 능력치")]
    public float baseHp = 20f;

    [Header("골드 드롭")]
    public int goldMin = 5;
    public int goldMax = 15;
    public int coinCountMin = 1;
    public int coinCountMax = 3;

    [Header("접촉 판정")]
    public bool contactHurtsPlayer = true;
    [Tooltip("플레이어와 부딪히면 스스로 소멸 (돌진형)")]
    public bool dieOnContact = false;

    [Header("연출")]
    public SpriteRenderer sr;
    public Color hitFlashColor = new Color(1f, 0.35f, 0.35f);
    public float deathFxScale = 1f;

    protected float hp, maxHp;
    protected float difficulty = 1f;
    protected float goldMul = 1f;
    protected bool isDead;
    protected Color baseColor = Color.white;
    float flashTimer;

    protected PlayerController Player => PlayerController.Instance;
    /// <summary>시간이 지날수록 적탄도 조금씩 빨라짐 (최대 1.6배)</summary>
    protected float BulletSpeedMul => Mathf.Min(1f + (difficulty - 1f) * 0.15f, 1.6f);
    protected virtual Color IdleColor => baseColor;

    protected virtual void Awake()
    {
        PhysicsUtil.SetupTriggerBody(gameObject, true);
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
        hp = maxHp = baseHp;
    }

    /// <summary>스포너가 생성 직후 호출 (Start보다 먼저)</summary>
    public virtual void Init(float difficulty, float goldMul)
    {
        this.difficulty = Mathf.Max(1f, difficulty);
        this.goldMul = goldMul;
        hp = maxHp = baseHp * this.difficulty;
    }

    protected virtual void Update()
    {
        if (isDead) return;
        float dt = Time.deltaTime;

        Move(dt);

        if (sr != null)
        {
            if (flashTimer > 0f) { flashTimer -= dt; sr.color = hitFlashColor; }
            else sr.color = IdleColor;
        }

        if (IsGone()) Destroy(gameObject);
    }

    protected abstract void Move(float dt);

    protected virtual bool IsGone()
    {
        Vector3 p = transform.position;
        return p.y < ScreenBounds.Bottom - 1.5f ||
               p.x < ScreenBounds.Left - 2.5f ||
               p.x > ScreenBounds.Right + 2.5f;
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        hp -= damage;
        flashTimer = 0.06f;

        if (hp <= 0f) Die(true);
        else SfxPlayer.Play(SfxType.Hit, 0.3f);
    }

    /// <summary>블랙홀 흡입</summary>
    public void Pull(Vector2 delta)
    {
        if (!isDead) transform.position += (Vector3)delta;
    }

    protected virtual void Die(bool killedByPlayer)
    {
        if (isDead) return;
        isDead = true;

        if (killedByPlayer)
        {
            DropGold();
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnEnemyKilled();
                gm.SpawnFx(gm.enemyDeathFx, transform.position, deathFxScale);
            }
            SfxPlayer.Play(SfxType.EnemyDie, 0.6f);
        }

        Destroy(gameObject);
    }

    /// <summary>총 골드를 랜덤 개수의 코인으로 나눠서 뿌린다 (값이 클수록 큰 코인)</summary>
    void DropGold()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        int total = Mathf.Max(1, Mathf.RoundToInt(Random.Range(goldMin, goldMax + 1) * goldMul));
        int count = Mathf.Clamp(Random.Range(coinCountMin, coinCountMax + 1), 1, total);
        int remaining = total;

        for (int i = 0; i < count; i++)
        {
            int coinsLeftAfter = count - 1 - i;
            int value = (i == count - 1)
                ? remaining
                : Mathf.RoundToInt(total / (float)count * Random.Range(0.4f, 1.6f));
            value = Mathf.Clamp(value, 1, remaining - coinsLeftAfter);
            remaining -= value;

            Vector2 offset = Random.insideUnitCircle * 0.3f;
            gm.SpawnCoin(transform.position + (Vector3)offset, value);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (isDead || !contactHurtsPlayer) return;

        var rb = other.attachedRigidbody;
        if (rb == null || !rb.TryGetComponent(out PlayerController player)) return;

        if (player.Hit() && dieOnContact) Die(false);
    }
}
