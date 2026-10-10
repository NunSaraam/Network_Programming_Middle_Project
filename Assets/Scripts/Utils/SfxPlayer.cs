using UnityEngine;

public enum SfxType { Shoot, Hit, EnemyDie, Coin, PlayerHit, Dive, Evolve, Fail, GameOver }

/// <summary>각 씬에 하나 배치. 클립이 비어 있으면 그냥 무음으로 동작한다.</summary>
public class SfxPlayer : MonoBehaviour
{
    static SfxPlayer instance;

    public AudioClip shoot, hit, enemyDie, coin, playerHit, dive, evolve, fail, gameOver;
    [Range(0f, 1f)] public float masterVolume = 0.8f;

    AudioSource source;

    void Awake()
    {
        instance = this;
        source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public static void Play(SfxType type, float volume = 1f)
    {
        if (instance == null) return;
        var clip = instance.GetClip(type);
        if (clip != null) instance.source.PlayOneShot(clip, volume * instance.masterVolume);
    }

    AudioClip GetClip(SfxType t)
    {
        switch (t)
        {
            case SfxType.Shoot:     return shoot;
            case SfxType.Hit:       return hit;
            case SfxType.EnemyDie:  return enemyDie;
            case SfxType.Coin:      return coin;
            case SfxType.PlayerHit: return playerHit;
            case SfxType.Dive:      return dive;
            case SfxType.Evolve:    return evolve;
            case SfxType.Fail:      return fail;
            case SfxType.GameOver:  return gameOver;
            default:                return null;
        }
    }
}
