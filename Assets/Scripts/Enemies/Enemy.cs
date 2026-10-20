using UnityEngine;

/// <summary>
/// 모든 몬스터의 공통 기반: 체력(+체력바), 피격 연출, 골드 드롭, 몸통 박치기 판정, 엘리트 변종.
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

    [Header("체력바")]
    [Tooltip("처음 맞는 순간 머리 위에 체력바 표시")]
    public bool showHpBar = true;
    public Color hpBarColor = new Color(1f, 0.3f, 0.35f);
    public Color eliteHpBarColor = new Color(1f, 0.8f, 0.2f);

    protected float hp, maxHp;
    protected float difficulty = 1f;
    protected float goldMul = 1f;
    protected bool isDead;
    protected Color baseColor = Color.white;
    float flashTimer;

    public bool IsElite { get; private set; }

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
    public virtual void Init(float difficulty, float goldMul, float hpScale = 1f)
    {
        this.difficulty = Mathf.Max(1f, difficulty);
        this.goldMul = goldMul;
        hp = maxHp = baseHp * this.difficulty * hpScale;
    }

    /// <summary>엘리트 변종: 더 크고, 단단하고, 골드를 많이 준다</summary>
    public void MakeElite(float hpMul, float goldMultiplier, float sizeMul, Color tint)
    {
        IsElite = true;
        maxHp *= hpMul;
        hp = maxHp;
        goldMul *= goldMultiplier;
        transform.localScale *= sizeMul;
        baseColor = tint;
        deathFxScale *= 1.6f;
        if (showHpBar) CreateHpBar();
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

        if (showHpBar && hpBarRoot == null) CreateHpBar();
        UpdateHpBar();

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
        int count = Random.Range(coinCountMin, coinCountMax + 1) * (IsElite ? 2 : 1);
        count = Mathf.Clamp(count, 1, total);
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

    // ===== 체력바 (프리팹 없이 코드로 생성) =====

    static Sprite barSprite;
    Transform hpBarRoot, hpFill;
    float hpBarWidth, hpBarOffsetY;

    static Sprite BarSprite
    {
        get
        {
            // 1x1 유닛 흰 사각형, 피벗 왼쪽 가운데 → 가로 스케일로 길이 조절
            if (barSprite == null)
                barSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0f, 0.5f), 4f);
            return barSprite;
        }
    }

    protected void CreateHpBar()
    {
        if (hpBarRoot != null) return;

        Bounds b = sr != null ? sr.bounds : new Bounds(transform.position, Vector3.one * 0.6f);
        hpBarWidth = Mathf.Clamp(b.size.x * 0.8f, 0.5f, 1.8f);
        hpBarOffsetY = b.extents.y + 0.12f;

        hpBarRoot = new GameObject("HpBar").transform;
        Transform bg = NewBarPart("Bg", new Color(0f, 0f, 0f, 0.65f), 100);
        bg.localScale = new Vector3(hpBarWidth + 0.06f, 0.16f, 1f);
        bg.localPosition = new Vector3(-(hpBarWidth + 0.06f) * 0.5f, 0f, 0f);

        hpFill = NewBarPart("Fill", IsElite ? eliteHpBarColor : hpBarColor, 101);
        hpFill.localPosition = new Vector3(-hpBarWidth * 0.5f, 0f, 0f);

        UpdateHpBar();
        LateUpdate();
    }

    Transform NewBarPart(string name, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(hpBarRoot, false);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = BarSprite;
        r.color = color;
        r.sortingOrder = order;
        return go.transform;
    }

    void UpdateHpBar()
    {
        if (hpFill != null)
            hpFill.localScale = new Vector3(hpBarWidth * Mathf.Clamp01(hp / maxHp), 0.1f, 1f);
    }

    protected virtual void LateUpdate()
    {
        // 몬스터가 회전해도 체력바는 항상 머리 위에 수평으로
        if (hpBarRoot != null) hpBarRoot.position = transform.position + Vector3.up * hpBarOffsetY;
    }

    protected virtual void OnDestroy()
    {
        if (hpBarRoot != null) Destroy(hpBarRoot.gameObject);
    }
}
