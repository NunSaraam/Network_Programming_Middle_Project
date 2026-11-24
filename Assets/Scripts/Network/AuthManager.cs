using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

/// <summary>
/// [실습 5 / 3.5~3.6] Firebase 익명(게스트) 로그인. Login 씬에만 둔다.
/// 로그인이 끝나면 Lobby 씬으로 이동 (수업 예제의 Menu 씬 = 이 게임의 Lobby 씬).
/// Uid, IdToken, Name, Logout 은 static 이라 다른 씬에서 AuthManager.Uid 처럼 쓴다.
/// </summary>
public class AuthManager : MonoBehaviour
{
    public string apiKey;
    public TMP_Text statusText;                                    // Login 씬의 상태 글자
    [Tooltip("로그인 후 이동할 씬 (Build Profiles 의 Scene List 에 있어야 함)")]
    public string nextScene = "Lobby";
    public static string Uid = "", IdToken = "", Name = "";         // static: 씬이 바뀌어도 남음

    public void GuestLogin() { statusText.text = "로그인 중..."; StartCoroutine(SignIn()); }   // 게스트 로그인 버튼
    public static void Logout() { Uid = ""; IdToken = ""; Name = ""; SceneManager.LoadScene("Login"); }

    IEnumerator SignIn()
    {
        string saved = PlayerPrefs.GetString("refreshToken", "");       // 저장해 둔 증명서. 처음이면 빈 문자열
        string signUp = "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=" + apiKey;
        string refresh = "https://securetoken.googleapis.com/v1/token?key=" + apiKey;
        UnityWebRequest req = saved == ""                              // 처음: 새 손님 가입 / 아니면: 같은 손님의 새 토큰
            ? UnityWebRequest.Post(signUp, "{\"returnSecureToken\":true}", "application/json")
            : UnityWebRequest.Post(refresh, "grant_type=refresh_token&refresh_token=" + saved, "application/x-www-form-urlencoded");
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success) { PlayerPrefs.DeleteKey("refreshToken"); statusText.text = req.error; yield break; }
        AuthResponse r = JsonUtility.FromJson<AuthResponse>(req.downloadHandler.text);
        Uid = saved == "" ? r.localId : r.user_id;  IdToken = saved == "" ? r.idToken : r.id_token;
        PlayerPrefs.SetString("refreshToken", saved == "" ? r.refreshToken : r.refresh_token); PlayerPrefs.Save();
        Name = "손님 " + Uid.Substring(0, 6); SceneManager.LoadScene(nextScene);   // 로그인 끝 → Lobby 씬으로
    }
}

[System.Serializable] public class AuthResponse { public string idToken, localId, refreshToken, id_token, user_id, refresh_token; }
