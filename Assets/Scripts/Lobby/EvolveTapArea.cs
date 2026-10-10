using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// [로비] 화면을 덮는 투명 Image에 붙인다. 탭하면 확률 강화 1회 시도.
/// ※ 출격 버튼은 이 오브젝트보다 Hierarchy에서 아래(=화면 위)에 있어야 클릭이 가로채지지 않는다.
/// </summary>
public class EvolveTapArea : MonoBehaviour, IPointerDownHandler
{
    public LobbyManager lobby;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (lobby != null) lobby.OnTapEnhance();
    }
}
