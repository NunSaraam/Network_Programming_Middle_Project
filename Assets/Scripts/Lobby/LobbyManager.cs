using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 메인 로비: 보유 골드 / 현재 물고기 정보 표시, 탭 확률 강화(성공 시 진화), 출격.
/// </summary>
public class LobbyManager : MonoBehaviour
{
    [Header("물고기 표시")]
    public Image fishImage;

    [Header("텍스트")]
    public TMP_Text goldText;
    public TMP_Text stageText;     // "Lv.3 / 20"
    public TMP_Text nameText;      // "블루탱 (연안)"
    public TMP_Text descText;      // "3갈래 확산탄"
    public TMP_Text statText;      // 공격력 / 공속 / 패턴
    public TMP_Text costText;      // 다음 단계 · 성공 확률 · 강화 비용
    public TMP_Text bestText;      // 최고 기록
    public TMP_Text hintText;      // 안내 메시지

    [Header("버튼")]
    public Button startButton;

    [Header("연출")]
    public GameObject evolveFxPrefab;   // 선택: UI용 이펙트 프리팹 (Canvas 하위에 생성)
    [Tooltip("탭 후 결과가 나오기까지 떨리는 시간(초)")]
    public float enhanceChargeTime = 0.6f;
    public float bobAmplitude = 12f;
    public float bobSpeed = 2f;
    public Color affordableColor = new Color(0.4f, 1f, 0.5f);
    public Color notEnoughColor = new Color(1f, 0.45f, 0.45f);

    [Header("씬")]
    public string gameSceneName = "Game";

    RectTransform fishRect;
    Vector2 fishBasePos;
    Vector3 fishBaseScale;
    Vector2 shakeOffset;
    float punch;
    Coroutine animRoutine, hintRoutine;
    string defaultHint;
    bool enhancing;

    void Start()
    {
        Time.timeScale = 1f;
        GameData.Load();

        fishRect = fishImage.rectTransform;
        fishBasePos = fishRect.anchoredPosition;
        fishBaseScale = fishRect.localScale;

        if (startButton != null) startButton.onClick.AddListener(StartGame);
        Refresh();
    }

    void Update()
    {
        // 둥실둥실 대기 모션 + 흔들림 + 진화 펀치
        float t = Time.unscaledTime;
        float bob = Mathf.Sin(t * bobSpeed) * bobAmplitude;
        fishRect.anchoredPosition = fishBasePos + new Vector2(0f, bob) + shakeOffset;

        // 헤엄 애니메이션: 좌우로 꼬리 치듯 흔들림 + 살짝 늘었다 줄었다
        fishRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 4f) * 6f);
        float squash = 1f + Mathf.Sin(t * 8f) * 0.035f;
        Vector3 s = fishBaseScale * StageScale(GameData.Stage) * (1f + punch);
        fishRect.localScale = new Vector3(s.x * squash, s.y / squash, s.z);
    }

    static float StageScale(int stage) => 1f + (stage - 1) * 0.025f;

    // ===== 탭 강화 =====

    public void OnTapEnhance()
    {
        if (enhancing) return;

        if (GameData.IsMaxStage)
        {
            ShowTempHint("최종 진화 완료! 더 이상 강화할 수 없습니다.");
            return;
        }

        int cost = GameData.NextEnhanceCost;
        if (GameData.Gold < cost)
        {
            PlayAnim(FailShake());
            SfxPlayer.Play(SfxType.Fail);
            ShowTempHint($"골드가 부족해요! {cost - GameData.Gold:N0} G 더 필요");
            return;
        }

        StartCoroutine(EnhanceRoutine());
    }

    IEnumerator EnhanceRoutine()
    {
        enhancing = true;

        // 골드 차감 + 확률 판정 + 저장을 먼저 끝낸다 (연출 중에 탭을 닫아도 결과 보존)
        float rate = GameData.CurrentSuccessRate;
        float pityPerFail = GameData.CurrentPityPerFail; // 판정 전 단계 기준 (실패 시 이만큼 오름)
        EnhanceResult result = GameData.TryEnhance();
        if (goldText != null) goldText.text = $"{GameData.Gold:N0} G";

        // 두근두근: 점점 세게 떨리다가 결과 공개
        if (hintRoutine != null) { StopCoroutine(hintRoutine); hintRoutine = null; }
        if (hintText != null) hintText.text = $"강화 중...  (성공 확률 {FishEvolutionTable.FormatRate(rate)})";
        if (animRoutine != null) { StopCoroutine(animRoutine); animRoutine = null; punch = 0f; }
        for (float t = 0f; t < enhanceChargeTime; t += Time.unscaledDeltaTime)
        {
            float k = t / enhanceChargeTime;
            shakeOffset = Random.insideUnitCircle * (2f + 10f * k);
            yield return null;
        }
        shakeOffset = Vector2.zero;

        Refresh();
        if (result == EnhanceResult.Success)
        {
            var s = FishEvolutionTable.Get(GameData.Stage);
            PlayAnim(EvolvePunch());
            SfxPlayer.Play(SfxType.Evolve);
            if (evolveFxPrefab != null)
            {
                var fx = Instantiate(evolveFxPrefab, fishRect.parent);
                fx.transform.position = fishRect.position;
                Destroy(fx, 2f);
            }
            ShowTempHint($"강화 성공!  {s.Name}(으)로 진화 ({FishEvolutionTable.PatternName(s.Pattern)})");
        }
        else
        {
            PlayAnim(FailShake());
            SfxPlayer.Play(SfxType.Fail);
            ShowTempHint($"강화 실패...  다음 확률 {FishEvolutionTable.FormatRate(GameData.CurrentSuccessRate)} (+{FishEvolutionTable.FormatRate(pityPerFail)})");
        }

        enhancing = false;
    }

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // ===== 표시 갱신 =====

    void Refresh()
    {
        int stage = GameData.Stage;
        var s = FishEvolutionTable.Get(stage);

        FishVisuals.Apply(fishImage, stage);

        if (goldText != null)  goldText.text = $"{GameData.Gold:N0} G";
        if (stageText != null) stageText.text = $"Lv.{stage} / {FishEvolutionTable.MaxStage}";
        if (nameText != null)  nameText.text = $"{s.Name} ({s.Habitat})";
        if (descText != null)  descText.text = s.Description;
        if (statText != null)
        {
            string keep = GameData.ResetStageOnDeath
                ? $"  ·  사망 시 Lv.{(GameData.KeepCheckpointOnDeath ? FishEvolutionTable.GetCheckpoint(stage) : 1)}"
                : "";
            statText.text = $"공격력 {s.Damage:0}   공속 {1f / s.FireInterval:0.0}/s\n" +
                            $"패턴 {FishEvolutionTable.PatternName(s.Pattern)} x{s.Count}{keep}";
        }
        if (bestText != null)  bestText.text = $"최고 Lv.{GameData.BestStage}  ·  {GameData.BestRunGold:N0} G";

        bool affordable = false;
        if (costText != null)
        {
            if (GameData.IsMaxStage)
            {
                costText.text = "MAX";
                costText.color = affordableColor;
            }
            else
            {
                int cost = GameData.NextEnhanceCost;
                affordable = GameData.Gold >= cost;
                var next = FishEvolutionTable.Get(stage + 1);
                float bonus = GameData.CurrentPityBonus;
                string bonusText = bonus > 0.001f ? $" (+{FishEvolutionTable.FormatRate(bonus)})" : "";
                costText.text = $"다음: {next.Name}  ·  성공 {FishEvolutionTable.FormatRate(GameData.CurrentSuccessRate)}{bonusText}\n강화 비용 {cost:N0} G";
                costText.color = affordable ? affordableColor : notEnoughColor;
            }
        }
        else
        {
            affordable = !GameData.IsMaxStage && GameData.Gold >= GameData.NextEnhanceCost;
        }

        defaultHint = GameData.IsMaxStage ? "최종 진화 완료! 최고 기록에 도전하세요."
                    : affordable ? "화면을 탭하면 강화합니다!"
                    : "출격해서 골드를 모으세요!";
        if (hintRoutine == null && hintText != null) hintText.text = defaultHint;
    }

    void ShowTempHint(string msg)
    {
        if (hintText == null) return;
        if (hintRoutine != null) StopCoroutine(hintRoutine);
        hintRoutine = StartCoroutine(HintRoutine(msg));
    }

    IEnumerator HintRoutine(string msg)
    {
        hintText.text = msg;
        yield return new WaitForSecondsRealtime(1.5f);
        hintText.text = defaultHint;
        hintRoutine = null;
    }

    // ===== 연출 =====

    void PlayAnim(IEnumerator routine)
    {
        if (animRoutine != null) StopCoroutine(animRoutine);
        shakeOffset = Vector2.zero;
        punch = 0f;
        animRoutine = StartCoroutine(routine);
    }

    IEnumerator EvolvePunch()
    {
        const float duration = 0.4f;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float n = t / duration;
            punch = Mathf.Sin(n * Mathf.PI * 3f) * 0.35f * (1f - n); // 탄성 있게 커졌다 작아짐
            yield return null;
        }
        punch = 0f;
        animRoutine = null;
    }

    IEnumerator FailShake()
    {
        const float duration = 0.3f;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float n = 1f - t / duration;
            shakeOffset = new Vector2(Random.Range(-1f, 1f) * 15f * n, 0f);
            yield return null;
        }
        shakeOffset = Vector2.zero;
        animRoutine = null;
    }

    // 인스펙터 우클릭 메뉴 → 저장 데이터 초기화 (테스트용)
    [ContextMenu("Reset Save Data")]
    void ResetSaveData()
    {
        GameData.ResetAll();
        if (Application.isPlaying) Refresh();
    }
}
