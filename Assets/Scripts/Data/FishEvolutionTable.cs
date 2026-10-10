using UnityEngine;

public enum AttackPattern
{
    Single,      // 단발
    Parallel,    // 평행 N갈래
    Fan,         // 부채꼴 N갈래
    Radial,      // 전방위 방사탄
    SpinPierce,  // 관통 회전탄
    Laser,       // 일직선 레이저
    Shard,       // 거대 파편 (명중 시 분열)
    Tentacle,    // 촉수 채찍 (휘두르기)
    BlackHole,   // 블랙홀 (흡입 + 지속 피해 + 적탄 소거)
}

public class FishStage
{
    public int Stage;
    public string Name;
    public string Habitat;
    public AttackPattern Pattern;
    public float Damage;        // 1발(또는 1틱) 데미지
    public float FireInterval;  // 발사 간격(초)
    public int Count;           // 갈래 수
    public float Spread;        // Parallel: 탄 간격(유닛) / 그 외: 부채꼴·휘두르기 각도(도)
    public float Scale;         // 투사체 크기(레이저·촉수는 굵기)
    public float Speed;         // 투사체 속도
    public Color Tint;          // 탄 색상 + 스프라이트가 없을 때 대체 색상
    public string Description;
}

/// <summary>
/// 20단계 진화 테이블. 밸런스는 여기 숫자만 고치면 된다.
/// </summary>
public static class FishEvolutionTable
{
    public const int MaxStage = 20;

    // 강화 비용: BaseCost * CostGrowth^(현재단계-1)  → 1→2단계 200G ... 19→20단계 약 7,200G
    public const int BaseCost = 200;
    public const float CostGrowth = 1.22f;

    // 강화 성공 확률(%): 1→2단계 100% (첫 강화는 무조건 성공), 이후 단계마다 -5%, 최저 20%
    public const int MinSuccessRate = 20;
    public const int RateDropPerStage = 5;
    // 실패 보정: 실패할 때마다 다음 시도 확률 +5% (성공하면 초기화)
    public const int PityBonusPerFail = 5;

    static readonly FishStage[] stages =
    {
        //  단계  이름               서식지   패턴                       데미지 간격  갈래 퍼짐  크기  속도   색상       설명
        S(1,  "플랑크톤",          "연안",  AttackPattern.Single,      10,  0.35f, 1,  0,    0.6f, 9f,   "#9BE7FF", "작은 단발 기포탄"),
        S(2,  "흰동가리",          "연안",  AttackPattern.Parallel,    10,  0.32f, 2,  0.35f,0.65f,9f,   "#FF8C42", "2갈래 평행탄"),
        S(3,  "블루탱",            "연안",  AttackPattern.Fan,         11,  0.30f, 3,  20,   0.7f, 9.5f, "#3D7BFF", "3갈래 확산탄"),
        S(4,  "가시복어",          "연안",  AttackPattern.Radial,      12,  0.45f, 10, 360,  0.75f,8f,   "#E8D36A", "전방위 가시 방사탄"),
        S(5,  "청새치",            "연안",  AttackPattern.Parallel,    15,  0.22f, 3,  0.3f, 0.8f, 12f,  "#4FA3D1", "고속 3연장 직사탄"),
        S(6,  "쏠배감펭",          "심해",  AttackPattern.Fan,         16,  0.28f, 5,  50,   0.85f,10f,  "#E0475B", "5갈래 부채꼴 독가시"),
        S(7,  "곰치",              "심해",  AttackPattern.Fan,         19,  0.26f, 7,  70,   0.9f, 10f,  "#7A8B3C", "7갈래 광역 부채꼴"),
        S(8,  "톱가오리",          "심해",  AttackPattern.SpinPierce,  30,  0.45f, 1,  0,    1.3f, 7f,   "#A0A6B0", "관통 회전 톱날"),
        S(9,  "귀상어",            "심해",  AttackPattern.SpinPierce,  34,  0.42f, 3,  30,   1.4f, 7.5f, "#6F7F96", "3연 관통 회전 톱날"),
        S(10, "아귀",              "심해",  AttackPattern.Laser,       20,  1.30f, 1,  0,    0.6f, 0f,   "#F2F27A", "일직선 발광 레이저"),
        S(11, "대왕오징어",        "심해",  AttackPattern.Laser,       22,  1.20f, 3,  30,   0.7f, 0f,   "#B57BFF", "3갈래 먹물 레이저"),
        S(12, "산갈치",            "심해",  AttackPattern.Laser,       28,  1.10f, 1,  0,    1.8f, 0f,   "#FF5FA2", "초광폭 오라 레이저"),
        S(13, "둔클레오스테우스",  "환상종", AttackPattern.Shard,      90,  0.55f, 1,  0,    1.8f, 6f,   "#8E6B4A", "거대 갑주 파편 (명중 시 8조각)"),
        S(14, "헬리코프리온",      "환상종", AttackPattern.Shard,     100,  0.50f, 2,  15,   2.0f, 6f,   "#C2A26B", "쌍 회전 톱니 파편"),
        S(15, "메갈로돈",          "환상종", AttackPattern.Shard,     110,  0.45f, 3,  30,   2.2f, 6.5f, "#5E7C8A", "3갈래 거대 이빨 파편"),
        S(16, "모사사우루스",      "환상종", AttackPattern.Shard,     120,  0.40f, 5,  60,   2.3f, 7f,   "#3C6E71", "5갈래 파편 폭풍"),
        S(17, "크라켄",            "환상종", AttackPattern.Tentacle,   45,  1.20f, 2,  150,  1.2f, 0f,   "#B23A48", "좌우 촉수 채찍"),
        S(18, "리바이어던",        "환상종", AttackPattern.Tentacle,   55,  1.00f, 4,  170,  1.6f, 0f,   "#2E86AB", "4연 해일 채찍"),
        S(19, "바하무트",          "환상종", AttackPattern.BlackHole,  40,  2.00f, 1,  0,    1.6f, 9f,   "#4B2E83", "심연 소용돌이"),
        S(20, "코스믹 고래",       "환상종", AttackPattern.BlackHole,  60,  1.60f, 2,  30,   2.4f, 10f,  "#9D4EDD", "은하를 삼키는 블랙홀"),
    };

    static FishStage S(int stage, string name, string habitat, AttackPattern pattern,
                       float damage, float interval, int count, float spread,
                       float scale, float speed, string hex, string desc)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return new FishStage
        {
            Stage = stage,
            Name = name,
            Habitat = habitat,
            Pattern = pattern,
            Damage = damage,
            FireInterval = interval,
            Count = count,
            Spread = spread,
            Scale = scale,
            Speed = speed,
            Tint = c,
            Description = desc,
        };
    }

    public static FishStage Get(int stage) => stages[Mathf.Clamp(stage, 1, MaxStage) - 1];

    /// <summary>현재 단계에서 강화 1회 시도 비용 (최종 단계면 0)</summary>
    public static int GetEnhanceCost(int currentStage)
    {
        if (currentStage >= MaxStage) return 0;
        float cost = BaseCost * Mathf.Pow(CostGrowth, currentStage - 1);
        return Mathf.RoundToInt(cost / 10f) * 10;
    }

    /// <summary>현재 단계의 기본 성공 확률(%)</summary>
    public static int GetBaseSuccessRate(int currentStage)
    {
        if (currentStage >= MaxStage) return 0;
        return Mathf.Max(MinSuccessRate, 100 - RateDropPerStage * (currentStage - 1));
    }

    /// <summary>실패 보정까지 더한 최종 성공 확률(%)</summary>
    public static int GetSuccessRate(int currentStage, int failStacks)
    {
        if (currentStage >= MaxStage) return 0;
        return Mathf.Min(100, GetBaseSuccessRate(currentStage) + failStacks * PityBonusPerFail);
    }

    public static string PatternName(AttackPattern p)
    {
        switch (p)
        {
            case AttackPattern.Single: return "단발";
            case AttackPattern.Parallel: return "평행탄";
            case AttackPattern.Fan: return "부채꼴";
            case AttackPattern.Radial: return "방사탄";
            case AttackPattern.SpinPierce: return "관통 회전탄";
            case AttackPattern.Laser: return "레이저";
            case AttackPattern.Shard: return "거대 파편";
            case AttackPattern.Tentacle: return "촉수 채찍";
            case AttackPattern.BlackHole: return "블랙홀";
            default: return p.ToString();
        }
    }
}
