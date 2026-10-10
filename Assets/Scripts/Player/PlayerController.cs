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
    public float moveSpeed = 7f;
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

        if (weapon != null) weapon.Setup(FishEvolutionTable.Get(stage), this);
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
            }
        }
    }

    void UpdateVisual(float dt)
    {
        if (body == null) return;

        diveVisual = Mathf.MoveTowards(diveVisual, IsDiving ? 1f : 0f, dt * 6f);

        Color c = Color.Lerp(bodyBaseColor, diveTint, diveVisual);
        if (invincibleTimer > 0f && Mathf.Repeat(Time.time * 12f, 1f) < 0.5f) c.a *= 0.3f; // 피격 후 깜빡임
        body.color = c;
        body.transform.localScale = Vector3.Lerp(bodyBaseScale, bodyBaseScale * diveScale, diveVisual);
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
