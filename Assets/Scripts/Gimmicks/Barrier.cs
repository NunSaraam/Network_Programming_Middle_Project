using UnityEngine;

/// <summary>
/// 기믹: 낙하하는 거대 산호/암석 장벽. 파괴 불가, 닿으면 피격 (잠수 시 통과).
/// 일반탄은 막고 관통 톱날은 통과한다.
/// 폭이 넓어져도 그림을 늘리지 않고 바위 덩어리를 여러 개 이어 붙여서, 빈틈이 잘 보이게 한다.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Barrier : MonoBehaviour
{
    [Tooltip("바위 그림 (루트의 자식 오브젝트 권장)")]
    public SpriteRenderer sr;
    public float fallSpeed = 2.4f;
    [Tooltip("히트박스를 외형보다 약간 작게 (공정한 판정)")]
    [Range(0.5f, 1f)] public float hitboxRatio = 0.9f;
    [Tooltip("바위 한 덩어리의 폭(유닛). 장벽 폭에 맞춰 개수가 정해짐")]
    public float segmentWidth = 1.2f;

    void Awake()
    {
        PhysicsUtil.SetupTriggerBody(gameObject, true);
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
    }

    public void Init(float width, float height, float speed)
    {
        fallSpeed = speed;
        var box = GetComponent<BoxCollider2D>();
        box.offset = Vector2.zero;

        // 그림이 루트에 직접 붙어 있으면 복제할 수 없으므로 예전 방식(늘리기)으로 처리
        if (sr == null || sr.gameObject == gameObject)
        {
            if (sr != null && sr.drawMode != SpriteDrawMode.Simple)
            {
                sr.size = new Vector2(width, height);
                box.size = sr.size * hitboxRatio;
            }
            else
            {
                transform.localScale = new Vector3(width, height, 1f);
                box.size = Vector2.one * hitboxRatio;
            }
            return;
        }

        // 루트는 늘리지 않고, 판정만 전체 폭으로
        transform.localScale = Vector3.one;
        box.size = new Vector2(width, height) * hitboxRatio;

        // 바위 덩어리 개수와 한 덩어리 크기
        int count = Mathf.Max(1, Mathf.RoundToInt(width / Mathf.Max(0.3f, segmentWidth)));
        float segW = width / count;
        Vector2 spriteSize = sr.sprite != null ? (Vector2)sr.sprite.bounds.size : Vector2.one;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f) spriteSize = Vector2.one;

        sr.drawMode = SpriteDrawMode.Simple;
        // 이웃 바위와 살짝 겹치게(1.1배) 해서 틈 없이 이어 보이게
        Vector3 segScale = new Vector3(segW * 1.1f / spriteSize.x, height / spriteSize.y, 1f);
        sr.transform.localScale = segScale;
        sr.transform.localPosition = new Vector3(-width * 0.5f + segW * 0.5f, 0f, 0f);
        sr.transform.localRotation = Quaternion.identity;

        for (int i = 1; i < count; i++)
        {
            var copy = Instantiate(sr.gameObject, transform);
            copy.transform.localScale = segScale;
            copy.transform.localPosition = new Vector3(-width * 0.5f + segW * (i + 0.5f), 0f, 0f);
            copy.transform.localRotation = Quaternion.identity;
            // 같은 바위가 반복돼 보이지 않도록 좌우 반전을 섞음
            var r = copy.GetComponent<SpriteRenderer>();
            if (r != null) r.flipX = (i % 2 == 1);
        }
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
