using UnityEngine;

/// <summary>장벽 경고 표시 등을 깜빡이게 한다.</summary>
public class Blink : MonoBehaviour
{
    public float interval = 0.12f;

    SpriteRenderer[] renderers;
    float timer;
    bool visible = true;

    void Awake() => renderers = GetComponentsInChildren<SpriteRenderer>();

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < interval) return;
        timer = 0f;
        visible = !visible;
        foreach (var r in renderers) r.enabled = visible;
    }
}
