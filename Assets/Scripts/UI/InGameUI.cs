using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>인게임 HUD (하트 3칸 / 골드 / 시간 / 단계) + 결과창.</summary>
public class InGameUI : MonoBehaviour
{
    [Header("HUD")]
    public Image[] heartImages;
    [Tooltip("비워두면 잃은 하트는 숨김 처리")]
    public Sprite heartFull;
    public Sprite heartEmpty;
    public TMP_Text goldText;
    public TMP_Text timeText;
    public TMP_Text stageText;

    [Header("결과창")]
    public GameObject resultPanel;
    public TMP_Text resultGoldText;
    public TMP_Text resultKillText;
    public TMP_Text resultTimeText;
    public TMP_Text resultTotalGoldText;
    [Tooltip("(선택) \"Lv.7 → Lv.1 초기화\" 안내")]
    public TMP_Text resultStageText;
    public GameObject newRecordLabel;
    public Button lobbyButton;

    PlayerController player;

    public void Init(PlayerController p)
    {
        player = p;
        if (player != null) player.OnHeartsChanged += RefreshHearts;

        if (resultPanel != null) resultPanel.SetActive(false);
        if (lobbyButton != null) lobbyButton.onClick.AddListener(() => GameManager.Instance.ReturnToLobby());

        var stage = FishEvolutionTable.Get(GameData.Stage);
        if (stageText != null) stageText.text = $"Lv.{stage.Stage} {stage.Name}";

        RefreshHearts();
        SetGold(0);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (timeText != null && gm != null) timeText.text = FormatTime(gm.RunTime);
    }

    void RefreshHearts()
    {
        if (player == null || heartImages == null) return;

        for (int i = 0; i < heartImages.Length; i++)
        {
            bool alive = i < player.Hearts;
            if (heartFull != null && heartEmpty != null)
                heartImages[i].sprite = alive ? heartFull : heartEmpty;
            else
                heartImages[i].enabled = alive;
        }
    }

    public void SetGold(int gold)
    {
        if (goldText != null) goldText.text = $"{gold:N0} G";
    }

    public void ShowResult(int runGold, int kills, float time, bool newRecord, int totalGold, int runStage)
    {
        if (resultPanel == null) return;
        resultPanel.SetActive(true);

        if (resultStageText != null)
        {
            resultStageText.text = GameData.ResetStageOnDeath && runStage > 1
                ? $"물고기 Lv.{runStage} → Lv.1 초기화"
                : $"물고기 Lv.{runStage}";
        }

        if (resultGoldText != null) resultGoldText.text = $"획득 골드  +{runGold:N0} G";
        if (resultKillText != null) resultKillText.text = $"처치  {kills:N0}";
        if (resultTimeText != null) resultTimeText.text = $"생존 시간  {FormatTime(time)}";
        if (resultTotalGoldText != null) resultTotalGoldText.text = $"보유 골드  {totalGold:N0} G";
        if (newRecordLabel != null) newRecordLabel.SetActive(newRecord);
    }

    static string FormatTime(float t)
    {
        int s = Mathf.FloorToInt(t);
        return $"{s / 60:00}:{s % 60:00}";
    }
}
