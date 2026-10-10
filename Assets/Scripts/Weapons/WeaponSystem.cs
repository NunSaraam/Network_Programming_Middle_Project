using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 자동 공격. 진화 단계의 AttackPattern에 따라 탄막을 생성한다.
/// </summary>
public class WeaponSystem : MonoBehaviour
{
    [Header("프리팹")]
    public Projectile projectilePrefab;
    public BeamAttack beamPrefab;
    public BlackHole blackHolePrefab;

    [Header("발사 위치 (비우면 자기 위치)")]
    public Transform firePoint;

    [Header("레이저 / 촉수")]
    public float laserLength = 14f;
    public float laserDuration = 0.8f;
    public float tentacleLength = 5f;
    public float tentacleDuration = 0.6f;

    [Header("특수탄")]
    public int shardSplitCount = 8;
    public float sawSpinSpeed = 900f;

    FishStage data;
    PlayerController owner;
    float cooldown;

    public void Setup(FishStage stage, PlayerController player)
    {
        data = stage;
        owner = player;
        cooldown = 0.3f;
        if (firePoint == null) firePoint = transform;
    }

    void Update()
    {
        if (data == null || owner == null || !owner.CanShoot) return;

        cooldown -= Time.deltaTime;
        if (cooldown > 0f) return;

        cooldown = data.FireInterval;
        Fire();
    }

    void Fire()
    {
        Vector2 origin = firePoint.position;
        SfxPlayer.Play(SfxType.Shoot, 0.25f);

        switch (data.Pattern)
        {
            case AttackPattern.Single:
                Shoot(origin, 0f);
                break;

            case AttackPattern.Parallel:
                for (int i = 0; i < data.Count; i++)
                {
                    float offset = (i - (data.Count - 1) * 0.5f) * data.Spread;
                    Shoot(origin + Vector2.right * offset, 0f);
                }
                break;

            case AttackPattern.Fan:
                foreach (float a in FanAngles(data.Count, data.Spread)) Shoot(origin, a);
                break;

            case AttackPattern.Radial:
                for (int i = 0; i < data.Count; i++) Shoot(origin, 360f / data.Count * i);
                break;

            case AttackPattern.SpinPierce:
                foreach (float a in FanAngles(data.Count, data.Spread))
                    Shoot(origin, a, pierce: 999, spin: sawSpinSpeed);
                break;

            case AttackPattern.Shard:
                foreach (float a in FanAngles(data.Count, data.Spread))
                    Shoot(origin, a, split: shardSplitCount);
                break;

            case AttackPattern.Laser:
                foreach (float a in FanAngles(data.Count, data.Spread))
                    SpawnBeam(a, 0f, laserLength, laserDuration);
                break;

            case AttackPattern.Tentacle:
                for (int i = 0; i < data.Count; i++)
                {
                    float sign = (i % 2 == 0) ? 1f : -1f;           // 좌→우, 우→좌 번갈아 휘두름
                    float length = tentacleLength * (1f + 0.3f * (i / 2));
                    SpawnBeam(sign * data.Spread * 0.5f, -sign * data.Spread, length, tentacleDuration);
                }
                break;

            case AttackPattern.BlackHole:
                if (blackHolePrefab == null) break;
                foreach (float a in FanAngles(data.Count, data.Spread))
                {
                    var bh = Instantiate(blackHolePrefab, origin, Quaternion.identity);
                    bh.Init(data.Damage, Dir(a), data.Speed, data.Scale, data.Tint);
                }
                break;
        }
    }

    void Shoot(Vector2 origin, float angle, int pierce = 0, float spin = 0f, int split = 0)
    {
        if (projectilePrefab == null) return;
        var p = Instantiate(projectilePrefab, origin, Quaternion.Euler(0f, 0f, angle));
        p.Init(data.Damage, Dir(angle), data.Speed, data.Scale, pierce, spin, split, data.Tint);
    }

    void SpawnBeam(float startAngle, float sweep, float length, float duration)
    {
        if (beamPrefab == null) return;
        var beam = Instantiate(beamPrefab, firePoint.position, Quaternion.identity);
        beam.Init(firePoint, data.Damage, startAngle, sweep, length, data.Scale, duration, data.Tint);
    }

    /// <summary>0도 = 위쪽, +각도 = 왼쪽(반시계)</summary>
    static Vector2 Dir(float angle) => Quaternion.Euler(0f, 0f, angle) * Vector2.up;

    static IEnumerable<float> FanAngles(int count, float spread)
    {
        if (count <= 1) { yield return 0f; yield break; }
        for (int i = 0; i < count; i++)
            yield return -spread * 0.5f + spread * i / (count - 1);
    }
}
