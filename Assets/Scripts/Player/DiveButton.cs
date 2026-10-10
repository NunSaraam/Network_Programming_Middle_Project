using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// [인게임] 홀드 버튼 - 누르고 있는 동안 잠수 무적.
/// OnPointerDown에서 켜고 OnPointerUp에서 끈다.
/// </summary>
public class DiveButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public PlayerController player;

    [Header("표시")]
    public Image buttonImage;
    [Tooltip("Image Type = Filled, Fill Method = Radial 360 권장")]
    public Image gaugeFill;
    public Color normalColor = Color.white;
    public Color pressedColor = new Color(0.5f, 0.7f, 1f);
    public Color lowGaugeColor = new Color(0.5f, 0.5f, 0.5f);

    bool held;

    public void OnPointerDown(PointerEventData eventData)
    {
        held = true;
        if (player != null) player.SetDiveHeld(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    void OnDisable() => Release();

    void Release()
    {
        held = false;
        if (player != null) player.SetDiveHeld(false);
    }

    void Update()
    {
        if (player == null) return;

        if (gaugeFill != null)
        {
            gaugeFill.fillAmount = player.DiveGauge01;
            gaugeFill.color = (player.IsDiving || player.CanStartDive) ? pressedColor : lowGaugeColor;
        }

        if (buttonImage != null)
            buttonImage.color = player.IsDiving ? pressedColor : (held ? lowGaugeColor : normalColor);
    }
}
