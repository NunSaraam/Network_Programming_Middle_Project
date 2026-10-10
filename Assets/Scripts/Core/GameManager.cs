using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 인게임(Game 씬) 진행 관리: 획득 골드 집계, 사망 시 즉시 정산 → 결과창 → 로비 복귀.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool IsPlaying => Instance != null && Instance.playing;

    [Header("참조")]
    public PlayerController player;
    public InGameUI ui;

    [Header("프리팹")]
    public Coin coinPrefab;
    public GameObject enemyDeathFx;   // 선택 (파티클 등)
    public GameObject playerHitFx;    // 선택

    [Header("씬")]
    public string lobbySceneName = "Lobby";

    [Header("게임오버 연출")]
    public float gameOverSlowMoScale = 0.3f;
    public float gameOverDelay = 1f;

    public int RunGold { get; private set; }
    public int Kills { get; private set; }
    public float RunTime { get; private set; }

    bool playing;
    int runStage;   // 이번 판에 출격한 단계 (사망 시 1단계로 초기화되기 전 값)

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        GameData.Load();
        runStage = GameData.Stage;
    }

    void Start()
    {
        playing = true;
        if (player != null) player.OnDied += HandlePlayerDied;
        if (ui != null) ui.Init(player);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (playing) RunTime += Time.deltaTime;
    }

    public void AddGold(int value)
    {
        if (!playing) return;
        RunGold += value;
        if (ui != null) ui.SetGold(RunGold);
    }

    public void OnEnemyKilled() => Kills++;

    public void SpawnCoin(Vector3 pos, int value)
    {
        if (coinPrefab == null) return;
        Instantiate(coinPrefab, pos, Quaternion.identity).Init(value);
    }

    public void SpawnFx(GameObject fxPrefab, Vector3 pos, float scale = 1f)
    {
        if (fxPrefab == null) return;
        var fx = Instantiate(fxPrefab, pos, Quaternion.identity);
        fx.transform.localScale *= scale;
        Destroy(fx, 2f);
    }

    void HandlePlayerDied()
    {
        if (!playing) return;
        playing = false;

        // 사망 즉시 정산 (탭을 닫아도 골드가 보존되도록 먼저 저장). 강화 단계는 여기서 1단계로 초기화됨
        bool newRecord = GameData.SubmitRun(RunGold);
        SfxPlayer.Play(SfxType.GameOver);
        StartCoroutine(GameOverRoutine(newRecord));
    }

    IEnumerator GameOverRoutine(bool newRecord)
    {
        Time.timeScale = gameOverSlowMoScale;
        yield return new WaitForSecondsRealtime(gameOverDelay);
        Time.timeScale = 0f;
        if (ui != null) ui.ShowResult(RunGold, Kills, RunTime, newRecord, GameData.Gold, runStage);
    }

    public void ReturnToLobby()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(lobbySceneName);
    }
}
