using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 물고기.
///  - 이동: PlayerInput(Behavior = Send Messages) → OnMove(InputValue)  [On-Screen Stick / 키보드]
///  - 잠수: DiveButton이 SetDiveHeld(true/false) 호출  [PC 테스트용으로 Space 키도 지원]
///  - 체력: 고정 하트 3칸, 회복 불가
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("참조")]
    [Tooltip("자식 오브젝트의 SpriteRenderer (히트박스와 외형 크기를 분리하기 위함)")]
    public SpriteRenderer body;
    public WeaponSystem weapon;

    [Header("이동")]
    public float moveSpeed = 5f;
    public Vector2 screenPadding = new Vector2(0.35f, 0.5f);

    [Header("체력 (고정 3칸, 회복 불가)")]
    public int maxHearts = 3;
    public float hitInvincibleTime = 1.5f;

    [Header("잠수 무적기")]
    public float diveGaugeMax = 100f;
    public float diveDrainPerSec = 40f;
    public float diveRegenPerSec = 12f;
    public float diveMinToStart = 20f;
    public bool canShootWhileDiving = false;
    public Color diveTint = new Color(0.25f, 0.35f, 0.9f, 0.35f);
    public float diveScale = 0.8f;
    [Tooltip("잠수 중 몸 주변에 표시되는 보호막 고리 (코드로 자동 생성)")]
    public Color diveShieldColor = new Color(0.45f, 1f, 1f, 0.9f);
    [Tooltip("물고기 크기 대비 보호막 고리 크기 배율")]
    public float diveShieldSize = 1.3f;

    [Header("헤엄 애니메이션")]
    public float swimWiggleAngle = 7f;
    public float swimWiggleSpeed = 7f;
    [Tooltip("좌우로 움직일 때 몸이 기우는 각도")]
    public float leanAngle = 18f;

    public int Hearts { get; private set; }
    public float DiveGauge { get; private set; }
    public float DiveGauge01 => DiveGauge / diveGaugeMax;
    public bool CanStartDive => DiveGauge >= diveMinToStart;
    public bool IsDiving { get; private set; }
    public bool IsDead { get; private set; }
    public bool CanShoot => !IsDead && (!IsDiving || canShootWhileDiving);

    public event Action OnHeartsChanged;
    public event Action OnDied;

    Rigidbody2D rb;
    Vector2 moveInput;
    bool diveButtonHeld;
    bool diveLocked;          // 게이지가 바닥나면 손을 뗄 때까지 재잠수 금지
    float invincibleTimer;
    float diveVisual;         // 0(수면) ~ 1(심해)
    Color bodyBaseColor = Color.white;
    Vector3 bodyBaseScale = Vector3.one;
    SpriteRenderer diveShield;
    float swimT, lean;

    void Awake()
    {
        Instance = this;
        rb = PhysicsUtil.SetupTriggerBody(gameObject, true);
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        Hearts = maxHearts;
        DiveGauge = diveGaugeMax;
    }

    void Start()
    {
        GameData.Load();
        int stage = GameData.Stage;

        if (body != null)
        {
            FishVisuals.Apply(body, stage);
            bodyBaseColor = body.color;
            body.transform.localScale *= 1f + (stage - 1) * 0.03f; // 진화할수록 외형만 커짐
            bodyBaseScale = body.transform.localScale;
        }

        CreateDiveShield();
        if (weapon != null) weapon.Setup(FishEvolutionTable.Get(stage), this);
    }

    float shieldScale = 1f;

    void CreateDiveShield()
    {
        // 물고기 이미지 크기(PPU·Body Scale)에 맞춰 고리 크기를 자동으로 정함
        float fishSize = body != null ? Mathf.Max(body.bounds.size.x, body.bounds.size.y) : 1f;
        shieldScale = Mathf.Max(0.5f, fishSize) * diveShieldSize;

        var go = new GameObject("DiveShield");
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * shieldScale;
        diveShield = go.AddComponent<SpriteRenderer>();
        diveShield.sprite = ShieldSprite;
        diveShield.sortingOrder = (body != null ? body.sortingOrder : 0) + 1;
        diveShield.color = new Color(diveShieldColor.r, diveShieldColor.g, diveShieldColor.b, 0f);
    }

    static Sprite shieldSprite;
    /// <summary>빛나는 고리 모양 스프라이트를 코드로 생성 (이미지 파일 불필요)</summary>
    static Sprite ShieldSprite
    {
        get
        {
            if (shieldSprite != null) return shieldSprite;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(d - 0.86f) / 0.1f);
                    float fill = d < 0.86f ? 0.22f * d : 0f;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Max(ring, fill) * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            shieldSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            return shieldSprite;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ===== 입력 =====

    // PlayerInput 컴포넌트가 "Move" 액션에 대해 SendMessage로 호출
    void OnMove(InputValue value)
    {
        moveInput = Vector2.ClampMagnitude(value.Get<Vector2>(), 1f);
    }

    public void SetDiveHeld(bool held) => diveButtonHeld = held;

    bool WantsDive()
    {
        bool key = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
        return diveButtonHeld || key;
    }

    // ===== 루프 =====

    void Update()
    {
        if (IsDead) return;
        float dt = Time.deltaTime;

        UpdateDive(dt);
        if (invincibleTimer > 0f) invincibleTimer -= dt;
        UpdateVisual(dt);
    }

    void FixedUpdate()
    {
        if (IsDead) return;

        Vector2 target = rb.position + moveInput * moveSpeed * Time.fixedDeltaTime;
        float minX = ScreenBounds.Left + screenPadding.x, maxX = ScreenBounds.Right - screenPadding.x;
        float minY = ScreenBounds.Bottom + screenPadding.y, maxY = ScreenBounds.Top - screenPadding.y;
        // 경계 계산이 잘못돼 범위가 뒤집히면 이동을 막지 않음 (카메라 설정 오류 대비)
        if (minX < maxX) target.x = Mathf.Clamp(target.x, minX, maxX);
        if (minY < maxY) target.y = Mathf.Clamp(target.y, minY, maxY);
        rb.MovePosition(target);
    }

    void UpdateDive(float dt)
    {
        bool want = WantsDive();
        if (!want) diveLocked = false;

        if (IsDiving)
        {
            DiveGauge -= diveDrainPerSec * dt;
            if (DiveGauge <= 0f)
            {
                DiveGauge = 0f;
                diveLocked = true;
                IsDiving = false;
            }
            else if (!want)
            {
                IsDiving = false;
            }
        }
        else
        {
            DiveGauge = Mathf.Min(diveGaugeMax, DiveGauge + diveRegenPerSec * dt);
            if (want && !diveLocked && CanStartDive)
            {
                IsDiving = true;
                SfxPlayer.Play(SfxType.Dive, 0.6f);
                FloatingText.Spawn(transform.position + Vector3.up * 0.7f, "무적!", diveShieldColor, 4f);
            }
        }
    }

    void UpdateVisual(float dt)
    {
        if (body == null) return;

        diveVisual = Mathf.MoveTowards(diveVisual, IsDiving ? 1f : 0f, dt * 6f);

        // 잠수 중에도 배경에 묻히지 않도록: 색은 살짝만 바꾸고 투명도는 60%까지만
        Color c = Color.Lerp(bodyBaseColor, diveTint, diveVisual * 0.5f);
        c.a = bodyBaseColor.a * Mathf.Lerp(1f, 0.6f, diveVisual);
        if (invincibleTimer > 0f && Mathf.Repeat(Time.time * 12f, 1f) < 0.5f) c.a *= 0.3f; // 피격 후 깜빡임
        body.color = c;

        // 헤엄: 꼬리 치듯 좌우로 흔들림 + 움직이는 방향으로 기울기 + 살짝 늘었다 줄었다
        swimT += dt * swimWiggleSpeed * (1f + moveInput.magnitude * 0.8f);
        lean = Mathf.Lerp(lean, -moveInput.x * leanAngle, dt * 8f);
        body.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(swimT) * swimWiggleAngle + lean);

        float squash = 1f + Mathf.Sin(swimT * 2f) * 0.04f;
        Vector3 s = Vector3.Lerp(bodyBaseScale, bodyBaseScale * diveScale, diveVisual);
        body.transform.localScale = new Vector3(s.x * squash, s.y / squash, s.z);

        // 보호막 고리: 잠수 중에만 맥박처럼 빛남
        if (diveShield != null)
        {
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 10f);
            Color sc = diveShieldColor; sc.a = diveShieldColor.a * diveVisual * pulse;
            diveShield.color = sc;
            diveShield.transform.localScale = Vector3.one * shieldScale * (1f + 0.06f * Mathf.Sin(Time.time * 8f));
        }
    }

    // ===== 피격 =====

    /// <summary>실제로 피해를 입었으면 true (잠수/무적 중이면 false → 탄이 통과)</summary>
    public bool Hit()
    {
        if (IsDead || IsDiving || invincibleTimer > 0f) return false;

        Hearts--;
        invincibleTimer = hitInvincibleTime;
        OnHeartsChanged?.Invoke();

        CameraShake.Shake(0.25f, 0.25f);
        SfxPlayer.Play(SfxType.PlayerHit);
        var gm = GameManager.Instance;
        if (gm != null) gm.SpawnFx(gm.playerHitFx, transform.position);

        if (Hearts <= 0) Die();
        return true;
    }

    void Die()
    {
        IsDead = true;
        IsDiving = false;
        moveInput = Vector2.zero;
        if (body != null) body.enabled = false;

        var gm = GameManager.Instance;
        if (gm != null) gm.SpawnFx(gm.enemyDeathFx, transform.position, 2f);
        CameraShake.Shake(0.5f, 0.4f);

        OnDied?.Invoke();
    }
}
