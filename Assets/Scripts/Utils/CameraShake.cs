using UnityEngine;

/// <summary>메인 카메라에 붙인다. 피격 시 화면 흔들림.</summary>
public class CameraShake : MonoBehaviour
{
    static CameraShake instance;

    Vector3 origin;
    float timeLeft, duration, magnitude;

    void Awake()
    {
        instance = this;
        origin = transform.localPosition;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public static void Shake(float duration, float magnitude)
    {
        if (instance == null) return;
        instance.duration = duration;
        instance.timeLeft = duration;
        instance.magnitude = magnitude;
    }

    void LateUpdate()
    {
        if (timeLeft > 0f)
        {
            timeLeft -= Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(timeLeft / duration);
            transform.localPosition = origin + (Vector3)(Random.insideUnitCircle * magnitude * k);
        }
        else
        {
            transform.localPosition = origin;
        }
    }
}
