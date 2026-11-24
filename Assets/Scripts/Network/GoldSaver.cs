using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

/// <summary>
/// [실습 7 / 3.10~3.11] 수업 예제의 ScoreSaver 를 골드용으로 바꾼 것.
/// 한 판 최고 획득 골드를 Realtime Database 의 scores/{uid} 에 { name, gold } 로 저장한다.
/// 경로는 수업의 DB 규칙(scores/$uid 만 쓰기 허용)을 그대로 쓰기 위해 "scores" 를 유지.
/// </summary>
public class GoldSaver : MonoBehaviour
{
    public string dbUrl;                                   // Realtime Database 주소. 끝에 / 없이
    public static GoldSaver Instance;                      // 다른 스크립트에서 GoldSaver.Instance 로 접근
    bool busy;                                             // 저장 요청이 진행 중인가

    void Awake() { Instance = this; }

    public void Save(int gold)
    {
        if (AuthManager.Uid == "" || busy) return;          // 로그인 전이거나 저장 중이면 보내지 않음
        busy = true; StartCoroutine(Put(gold));
    }

    IEnumerator Put(int gold)
    {
        string url = dbUrl + "/scores/" + AuthManager.Uid + ".json?auth=" + AuthManager.IdToken;
        UnityWebRequest get = UnityWebRequest.Get(url);
        yield return get.SendWebRequest();                 // 서버에 있는 내 기록을 먼저 읽음. 없으면 null
        if (get.result != UnityWebRequest.Result.Success)  // 읽기 실패: 기록을 모르는 채로 덮어쓰지 않음 (2.14)
        { Debug.Log("load failed " + get.error); busy = false; yield break; }
        string old = get.downloadHandler.text;
        int best = old.StartsWith("{") ? JsonUtility.FromJson<GoldData>(old).gold : 0;
        if (gold <= best) { busy = false; yield break; }   // 서버 기록이 더 많으면 덮어쓰지 않음
        string body = "{\"name\":\"" + AuthManager.Name + "\",\"gold\":" + gold + "}";
        UnityWebRequest req = UnityWebRequest.Put(url, body);
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();
        Debug.Log(req.result == UnityWebRequest.Result.Success ? "saved" : "save failed " + req.error);
        busy = false;
    }
}

[System.Serializable] public class GoldData { public string name; public int gold; }
