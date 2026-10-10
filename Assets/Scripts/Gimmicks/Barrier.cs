using UnityEngine;

/// <summary>
/// 기믹: 낙하하는 거대 산호/암석 장벽. 파괴 불가, 닿으면 피격 (잠수 시 통과).
/// 일반탄은 막고 관통 톱날은 통과한다.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Barrier : MonoBehaviour
{
    public SpriteRenderer sr;
    public float fallSpeed = 2.4f;
    [Tooltip("히트박스를 외형보다 약간 작게 (공정한 판정)")]
    [Range(0.5f, 1f)] public float hitboxRatio = 0.9f;

    void Awake()
    {
        PhysicsUtil.SetupTriggerBody(gameObject, true);
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
    }

    public void Init(float width, float height, float speed)
    {
        fallSpeed = speed;
        var box = GetComponent<BoxCollider2D>();

        if (sr != null && sr.drawMode != SpriteDrawMode.Simple)
        {
            // Draw Mode = Tiled/Sliced 이면 스케일 대신 크기를 늘려서 텍스처가 늘어나지 않게
            sr.size = new Vector2(width, height);
            box.size = sr.size * hitboxRatio;
        }
        else
        {
            transform.localScale = new Vector3(width, height, 1f);
            box.size = Vector2.one * hitboxRatio;
        }
        box.offset = Vector2.zero;
    }

    void Update()
    {
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;
        if (transform.position.y < ScreenBounds.Bottom - 3f) Destroy(gameObject);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        var rb = other.attachedRigidbody;
        if (rb != null && rb.TryGetComponent(out PlayerController player))
            player.Hit();
    }
}
