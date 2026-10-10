using UnityEngine;

public enum EnhanceResult { Success, Fail, NotEnoughGold, MaxStage }

/// <summary>
/// 영구 저장 데이터 (보유 골드 / 진화 단계 / 강화 실패 보정 / 최고 기록).
/// WebGL 빌드에서 PlayerPrefs는 브라우저 IndexedDB에 저장된다.
/// </summary>
public static class GameData
{
    public const int StartingGold = 1000;

    /// <summary>true면 사망 시 강화 단계가 1단계로 초기화된다 (골드는 유지)</summary>
    public static bool ResetStageOnDeath = true;

    const string KeyInit = "FE_Init";
    const string KeyGold = "FE_Gold";
    const string KeyStage = "FE_Stage";
    const string KeyFail = "FE_FailStacks";
    const string KeyBest = "FE_BestRunGold";
    const string KeyBestStage = "FE_BestStage";

    static bool loaded;

    public static int Gold { get; private set; }
    public static int Stage { get; private set; }          // 1 ~ 20
    public static int FailStacks { get; private set; }     // 현재 단계에서 연속 실패 횟수
    public static int BestRunGold { get; private set; }    // 한 판 최고 획득 골드
    public static int BestStage { get; private set; }      // 지금까지 도달한 최고 단계

    public static bool IsMaxStage => Stage >= FishEvolutionTable.MaxStage;
    public static int NextEnhanceCost => FishEvolutionTable.GetEnhanceCost(Stage);
    public static int CurrentSuccessRate => FishEvolutionTable.GetSuccessRate(Stage, FailStacks);
    /// <summary>실패 보정으로 실제로 더해진 확률(%) (100% 상한 반영)</summary>
    public static int CurrentPityBonus => CurrentSuccessRate - FishEvolutionTable.GetBaseSuccessRate(Stage);

    public static void Load()
    {
        if (loaded) return;
        loaded = true;

        if (PlayerPrefs.GetInt(KeyInit, 0) == 0)
        {
            // 최초 진입: 즉시 1차 강화를 체험할 수 있도록 1,000골드 지급
            PlayerPrefs.SetInt(KeyInit, 1);
            PlayerPrefs.SetInt(KeyGold, StartingGold);
            PlayerPrefs.SetInt(KeyStage, 1);
            PlayerPrefs.SetInt(KeyFail, 0);
            PlayerPrefs.SetInt(KeyBest, 0);
            PlayerPrefs.Save();
        }

        Gold = PlayerPrefs.GetInt(KeyGold, 0);
        Stage = Mathf.Clamp(PlayerPrefs.GetInt(KeyStage, 1), 1, FishEvolutionTable.MaxStage);
        FailStacks = Mathf.Max(0, PlayerPrefs.GetInt(KeyFail, 0));
        BestRunGold = PlayerPrefs.GetInt(KeyBest, 0);
        BestStage = Mathf.Max(Stage, PlayerPrefs.GetInt(KeyBestStage, 1));
    }

    public static void AddGold(int amount)
    {
        Load();
        Gold += Mathf.Max(0, amount);
        Save();
    }

    /// <summary>
    /// 골드를 소모하여 강화 1회 시도. 성공하면 다음 단계로 영구 진화.
    /// 실패해도 단계는 유지되고, 다음 시도 확률이 올라간다.
    /// </summary>
    public static EnhanceResult TryEnhance()
    {
        Load();
        if (IsMaxStage) return EnhanceResult.MaxStage;

        int cost = NextEnhanceCost;
        if (Gold < cost) return EnhanceResult.NotEnoughGold;

        Gold -= cost;
        bool success = Random.Range(0, 100) < CurrentSuccessRate;
        if (success)
        {
            Stage++;
            FailStacks = 0;
            BestStage = Mathf.Max(BestStage, Stage);
        }
        else
        {
            FailStacks++;
        }
        Save();
        return success ? EnhanceResult.Success : EnhanceResult.Fail;
    }

    /// <summary>
    /// 한 판 결과 정산 (사망 시 호출): 획득 골드 적립 → 최고 기록 갱신 → 강화 단계 초기화.
    /// 최고 골드 기록 갱신 시 true
    /// </summary>
    public static bool SubmitRun(int runGold)
    {
        Load();
        Gold += Mathf.Max(0, runGold);

        bool newRecord = runGold > BestRunGold;
        if (newRecord) BestRunGold = runGold;

        if (ResetStageOnDeath)
        {
            Stage = 1;
            FailStacks = 0;
        }

        Save();
        return newRecord;
    }

    public static void Save()
    {
        PlayerPrefs.SetInt(KeyGold, Gold);
        PlayerPrefs.SetInt(KeyStage, Stage);
        PlayerPrefs.SetInt(KeyFail, FailStacks);
        PlayerPrefs.SetInt(KeyBest, BestRunGold);
        PlayerPrefs.SetInt(KeyBestStage, BestStage);
        PlayerPrefs.Save();
    }

    /// <summary>테스트용 전체 초기화</summary>
    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(KeyInit);
        PlayerPrefs.DeleteKey(KeyGold);
        PlayerPrefs.DeleteKey(KeyStage);
        PlayerPrefs.DeleteKey(KeyFail);
        PlayerPrefs.DeleteKey(KeyBest);
        PlayerPrefs.DeleteKey(KeyBestStage);
        loaded = false;
        Load();
    }
}
