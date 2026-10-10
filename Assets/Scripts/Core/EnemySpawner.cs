using System.Collections;
using UnityEngine;

/// <summary>
/// 무한 웨이브 스포너. 시간이 지날수록 스폰 간격이 줄고, 적 체력/골드가 늘어난다.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("몬스터 프리팹 (4종)")]
    public Enemy rusherPrefab;
    public Enemy aimedShooterPrefab;
    public Enemy fanShooterPrefab;
    public Enemy seahorsePrefab;

    [Header("스폰 템포")]
    public float startInterval = 1.2f;
    public float minInterval = 0.35f;
    [Tooltip("이 시간(초)에 걸쳐 최소 간격까지 빨라짐")]
    public float rampDuration = 180f;
    [Range(0f, 1f)] public float squadChance = 0.25f;

    [Header("등장 시점(초)")]
    public float aimedShooterFrom = 8f;
    public float fanShooterFrom = 30f;

    [Header("황금 보물 해마")]
    public Vector2 seahorseInterval = new Vector2(18f, 30f);

    [Header("기믹: 산호/암석 장벽")]
    public Barrier barrierPrefab;
    [Tooltip("빨간 반투명 사각형 + Blink 컴포넌트 (선택)")]
    public GameObject warningPrefab;
    public float barrierStartTime = 25f;
    public Vector2 barrierInterval = new Vector2(12f, 20f);
    public float barrierGap = 2.2f;
    public float barrierHeight = 1.2f;
    public float barrierSpeed = 2.4f;
    public float warningTime = 1f;

    [Header("난이도 성장 (분당 증가량)")]
    public float hpGrowthPerMinute = 1f;    // 1분마다 적 체력 +100%
    public float goldGrowthPerMinute = 0.5f; // 1분마다 드롭 골드 +50%

    public float Elapsed { get; private set; }
    public float Difficulty => 1f + Elapsed / 60f * hpGrowthPerMinute;
    float GoldMultiplier => 1f + Elapsed / 60f * goldGrowthPerMinute;

    float spawnTimer = 1f;
    float seahorseTimer;
    float barrierTimer;

    void Start()
    {
        seahorseTimer = Random.Range(seahorseInterval.x, seahorseInterval.y);
        barrierTimer = barrierStartTime;
    }

    void Update()
    {
        if (!GameManager.IsPlaying) return;

        float dt = Time.deltaTime;
        Elapsed += dt;

        spawnTimer -= dt;
        if (spawnTimer <= 0f)
        {
            SpawnWave();
            float k = Mathf.Clamp01(Elapsed / rampDuration);
            spawnTimer = Mathf.Lerp(startInterval, minInterval, k) * Random.Range(0.8f, 1.2f);
        }

        seahorseTimer -= dt;
        if (seahorseTimer <= 0f)
        {
            SpawnSeahorse();
            seahorseTimer = Random.Range(seahorseInterval.x, seahorseInterval.y);
        }

        if (barrierPrefab != null)
        {
            barrierTimer -= dt;
            if (barrierTimer <= 0f)
            {
                StartCoroutine(BarrierRoutine());
                barrierTimer = Random.Range(barrierInterval.x, barrierInterval.y);
            }
        }
    }

    // ===== 일반 몬스터 =====

    void SpawnWave()
    {
        float wRusher = rusherPrefab != null ? 1f : 0f;
        float wAimed = (aimedShooterPrefab != null && Elapsed >= aimedShooterFrom)
            ? Mathf.Lerp(0.3f, 0.7f, (Elapsed - aimedShooterFrom) / 120f) : 0f;
        float wFan = (fanShooterPrefab != null && Elapsed >= fanShooterFrom)
            ? Mathf.Lerp(0.15f, 0.5f, (Elapsed - fanShooterFrom) / 120f) : 0f;

        float total = wRusher + wAimed + wFan;
        if (total <= 0f) return;

        float r = Random.value * total;
        if (r < wRusher) SpawnRushers();
        else if (r < wRusher + wAimed) Spawn(aimedShooterPrefab, RandomTopPosition(0.5f));
        else Spawn(fanShooterPrefab, RandomTopPosition(1f));
    }

    void SpawnRushers()
    {
        if (Random.value < squadChance)
        {
            // V자 편대
            int n = Random.Range(3, 6);
            float spacing = 0.8f;
            float width = spacing * (n - 1);
            float minX = ScreenBounds.Left + 0.4f;
            float maxX = Mathf.Max(minX, ScreenBounds.Right - 0.4f - width);
            float startX = Random.Range(minX, maxX);

            for (int i = 0; i < n; i++)
            {
                float y = ScreenBounds.Top + 0.6f + Mathf.Abs(i - (n - 1) * 0.5f) * 0.4f;
                Spawn(rusherPrefab, new Vector2(startX + i * spacing, y));
            }
        }
        else
        {
            Spawn(rusherPrefab, RandomTopPosition(0.4f));
        }
    }

    void SpawnSeahorse()
    {
        if (seahorsePrefab == null) return;
        bool fromLeft = Random.value < 0.5f;
        float x = fromLeft ? ScreenBounds.Left - 1f : ScreenBounds.Right + 1f;
        float y = ScreenBounds.ViewportY(Random.Range(0.6f, 0.85f));
        Spawn(seahorsePrefab, new Vector2(x, y));
    }

    Vector2 RandomTopPosition(float margin)
    {
        return new Vector2(Random.Range(ScreenBounds.Left + margin, ScreenBounds.Right - margin),
                           ScreenBounds.Top + 0.8f);
    }

    Enemy Spawn(Enemy prefab, Vector2 pos)
    {
        if (prefab == null) return null;
        var e = Instantiate(prefab, pos, Quaternion.identity);
        e.Init(Difficulty, GoldMultiplier);
        return e;
    }

    // ===== 장벽 기믹 =====

    IEnumerator BarrierRoutine()
    {
        float left = ScreenBounds.Left;
        float right = ScreenBounds.Right;
        float half = barrierGap * 0.5f;
        float gapCenter = Random.Range(left + half + 0.3f, right - half - 0.3f);
        float gapL = gapCenter - half;
        float gapR = gapCenter + half;

        if (warningPrefab != null)
        {
            ShowWarning(left, gapL);
            ShowWarning(gapR, right);
        }

        yield return new WaitForSeconds(warningTime);
        if (!GameManager.IsPlaying) yield break;

        float y = ScreenBounds.Top + barrierHeight * 0.5f + 0.2f;
        SpawnBarrier(left - 0.3f, gapL, y);
        SpawnBarrier(gapR, right + 0.3f, y);
    }

    void ShowWarning(float x0, float x1)
    {
        if (x1 - x0 < 0.1f) return;
        var w = Instantiate(warningPrefab, new Vector3((x0 + x1) * 0.5f, ScreenBounds.Top - 0.4f, 0f), Quaternion.identity);
        w.transform.localScale = new Vector3(x1 - x0, 0.5f, 1f);
        Destroy(w, warningTime);
    }

    void SpawnBarrier(float x0, float x1, float y)
    {
        float width = x1 - x0;
        if (width < 0.2f) return;
        var b = Instantiate(barrierPrefab, new Vector3((x0 + x1) * 0.5f, y, 0f), Quaternion.identity);
        b.Init(width, barrierHeight, barrierSpeed);
    }
}
