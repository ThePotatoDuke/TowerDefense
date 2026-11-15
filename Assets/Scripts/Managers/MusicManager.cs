using UnityEngine;
using UnityEngine.Serialization;

// The game scene's music: the gameplay track loops while playing, and the shop has its own music in
// levels that follow how much grass the player has, richer levels playing when there's more to spend.
// Levels 1 and up are layers of one tune the same length: they all start together when the shop opens and
// keep playing in step, silent but for the one that fits, so a change of level fades across without losing
// the beat. Level 0 (the poorest) is a tune of its own, any length, that loops by itself and fades across
// to and from the others. A special wave (every 10th) cuts the music for its banner's sound, then its own
// track fades in from the top once the banner has gone. The gameplay track pauses
// while the shop's open or a boss wave's on and carries on where it left off. When a wave that opens the
// shop is cleared the music fades out quickly, so its clear sound plays in a moment of quiet before the
// shop's music comes in. When the castle falls the music slows down to a stop, like a tape running out; when
// the player's killed it cuts off at once. The pause menu pauses it, and it carries on from there.
public class MusicManager : MonoBehaviour
{
    [SerializeField] private AudioClip gameplay;
    [Tooltip("Plays through special waves (every 10th) instead of the gameplay music.")]
    [SerializeField, FormerlySerializedAs("boss")] private AudioClip specialWave;
    [Tooltip("The shop's music, poorest first. Level 0 can be any length; the rest must be the same length " +
             "so they stay in step.")]
    [SerializeField] private AudioClip[] shopLevels;
    [Tooltip("Grass needed for each shop level, poorest first (the first is 0): the highest one reached plays.")]
    [SerializeField] private int[] shopLevelGrass = { 0, 40, 100, 200, 400 };

    [SerializeField, Range(0f, 1f)] private float volume = 0.6f;
    [Tooltip("Seconds to fade between the gameplay music and the shop's or the special wave's.")]
    [SerializeField] private float shopFadeTime = 0.6f;
    [Tooltip("Seconds to fade from one shop level to another.")]
    [SerializeField] private float levelFadeTime = 1f;
    [Tooltip("Seconds the music takes to fade out when a wave that opens the shop is cleared.")]
    [SerializeField] private float hushFadeTime = 0.3f;
    [Tooltip("Seconds the gameplay music takes to fade in when the game starts.")]
    [SerializeField] private float startFadeTime = 2f;
    [Tooltip("Seconds the special wave's music takes to fade in after its banner has gone.")]
    [SerializeField] private float specialWaveFadeTime = 1f;
    [Tooltip("Seconds the music takes to slow down to a stop when the castle falls.")]
    [SerializeField] private float castleDeathSlowTime = 2.5f;

    private AudioSource gameplaySource;
    private AudioSource bossSource;
    private AudioSource[] shopSources;
    private bool inShop;
    private bool bossWave;
    private bool bannerGone; // the special wave's banner has been and gone
    private bool hushed; // a shop's on its way
    private bool silenced; // the player's been killed or the castle's fallen: no more music
    private bool slowingDown; // the castle's fallen
    private float slowPitch = 1f;
    private float startFade; // time left on the opening fade in
    private int shopLevel = -1;
    private bool paused; // the pause menu's open
    private bool muted; // music switched off on the pause menu (AudioToggles)

    private void Awake()
    {
        gameplaySource = CreateSource(gameplay);
        bossSource = CreateSource(specialWave);
        shopSources = new AudioSource[shopLevels != null ? shopLevels.Length : 0];
        for (int i = 0; i < shopSources.Length; i++)
            shopSources[i] = CreateSource(shopLevels[i]);
    }

    private void OnEnable()
    {
        GameEvents.OnBossWaveStarted += HandleBossWaveStarted;
        GameEvents.OnBossWaveEnded += HandleBossWaveEnded;
        GameEvents.OnBossBannerEnded += HandleBannerEnded;
        GameEvents.OnShopComing += HandleShopComing;
        GameEvents.OnPlayerFatallyHit += Silence;
        GameEvents.OnCastleDied += HandleCastleDied;
    }

    private void OnDisable()
    {
        GameEvents.OnBossWaveStarted -= HandleBossWaveStarted;
        GameEvents.OnBossWaveEnded -= HandleBossWaveEnded;
        GameEvents.OnBossBannerEnded -= HandleBannerEnded;
        GameEvents.OnShopComing -= HandleShopComing;
        GameEvents.OnPlayerFatallyHit -= Silence;
        GameEvents.OnCastleDied -= HandleCastleDied;
    }

    private void HandleCastleDied() => slowingDown = true;

    // Every track drops in pitch together, then they stop
    private void SlowDown()
    {
        slowPitch = Mathf.MoveTowards(slowPitch, 0f, Time.unscaledDeltaTime / Mathf.Max(0.01f, castleDeathSlowTime));
        gameplaySource.pitch = bossSource.pitch = slowPitch;
        foreach (AudioSource source in shopSources)
            source.pitch = slowPitch;
        if (slowPitch <= 0f)
            Silence();
    }

    // Every track cut off at once
    private void Silence()
    {
        silenced = true;
        gameplaySource.Stop();
        bossSource.Stop();
        foreach (AudioSource source in shopSources)
            source.Stop();
    }

    private void HandleShopComing() => hushed = true;

    // The music cuts out at once, for the banner's sound
    private void HandleBossWaveStarted(BossWave wave)
    {
        bossWave = true;
        bannerGone = false;
        startFade = 0f;
        gameplaySource.volume = 0f;
        gameplaySource.Pause();
    }

    private void HandleBannerEnded()
    {
        bannerGone = true;
        if (silenced || bossSource.clip == null) return;
        bossSource.Stop();
        bossSource.volume = 0f;
        bossSource.Play();
    }

    private void HandleBossWaveEnded() => bossWave = false;

    private void Start()
    {
        if (gameplaySource.clip == null) return;
        gameplaySource.volume = 0f;
        startFade = startFadeTime;
        gameplaySource.Play();
    }

    private AudioSource CreateSource(AudioClip clip)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
        source.ignoreListenerPause = true; // the shop pauses the game, not its music
        if (clip != null)
            clip.LoadAudioData();
        return source;
    }

    private void Update()
    {
        if (muted == AudioToggles.MusicOn)
            SetMuted(!AudioToggles.MusicOn);
        if (silenced) return;
        if (PauseMenu.IsOpen != paused)
            SetPaused(PauseMenu.IsOpen);
        if (paused) return;
        if (slowingDown)
        {
            SlowDown();
            return;
        }

        bool shopOpen = Shop.IsOpen;
        if (shopOpen != inShop)
        {
            inShop = shopOpen;
            if (inShop)
            {
                hushed = false; // the shop's music takes over; the gameplay's comes back after
                StartShop();
            }
        }
        if (inShop)
            shopLevel = LevelFor(Currency.Grass);

        // Fades run on real time: the game's stopped while the shop's open
        float shopStep = Time.unscaledDeltaTime / Mathf.Max(0.01f, shopFadeTime);
        float levelStep = Time.unscaledDeltaTime / Mathf.Max(0.01f, levelFadeTime);
        float hushStep = Time.unscaledDeltaTime / Mathf.Max(0.01f, hushFadeTime);

        // The gameplay track waits (paused) through the shop, a special wave's banner and the wave, if it
        // has music
        bool gameplayOn = !inShop && !hushed && !(bossWave && (!bannerGone || bossSource.clip != null));
        if (gameplayOn)
            gameplaySource.UnPause();
        float gameplayStep = hushed ? hushStep : shopStep;
        if (startFade > 0f)
        {
            // The opening fade in, slower than the others
            startFade -= Time.unscaledDeltaTime;
            gameplayStep = Time.unscaledDeltaTime / Mathf.Max(0.01f, startFadeTime);
        }
        Fade(gameplaySource, gameplayOn ? volume : 0f, gameplayStep);
        if (!gameplayOn && gameplaySource.volume <= 0f && gameplaySource.isPlaying)
            gameplaySource.Pause();

        bool bossOn = bossWave && bannerGone && !inShop && !hushed;
        float specialStep = Time.unscaledDeltaTime / Mathf.Max(0.01f, specialWaveFadeTime);
        Fade(bossSource, bossOn ? volume : 0f, hushed ? hushStep : bossOn ? specialStep : shopStep);
        if (!bossWave && bossSource.volume <= 0f && bossSource.isPlaying)
            bossSource.Stop();

        for (int i = 0; i < shopSources.Length; i++)
        {
            AudioSource source = shopSources[i];
            float target = inShop && i == shopLevel ? volume : 0f;
            // Levels fade between each other at their pace; the whole shop fades out with the gameplay's
            Fade(source, target, inShop ? levelStep : shopStep);
            if (!inShop && source.volume <= 0f && source.isPlaying)
                source.Stop();
        }
    }

    // Every track stops where it is, and carries on from there (ones that weren't playing stay as they were)
    private void SetPaused(bool pause)
    {
        paused = pause;
        Pause(gameplaySource, pause);
        Pause(bossSource, pause);
        foreach (AudioSource source in shopSources)
            Pause(source, pause);
    }

    // Switched off, every track carries on silently, so switching back on comes in where it would be
    private void SetMuted(bool mute)
    {
        muted = mute;
        gameplaySource.mute = bossSource.mute = mute;
        foreach (AudioSource source in shopSources)
            source.mute = mute;
    }

    private static void Pause(AudioSource source, bool pause)
    {
        if (pause)
            source.Pause();
        else
            source.UnPause();
    }

    // Every level starts at once, silent, so the layers stay in step; the one that fits fades in
    private void StartShop()
    {
        double start = AudioSettings.dspTime + 0.1;
        foreach (AudioSource source in shopSources)
        {
            if (source.clip == null) continue;
            source.Stop();
            source.volume = 0f;
            source.PlayScheduled(start);
        }
        shopLevel = LevelFor(Currency.Grass);
    }

    private int LevelFor(int grass)
    {
        int level = 0;
        int count = Mathf.Min(shopSources.Length, shopLevelGrass != null ? shopLevelGrass.Length : 0);
        for (int i = 0; i < count; i++)
            if (grass >= shopLevelGrass[i])
                level = i;
        return level;
    }

    // A step of a fade that goes from silent to full volume in one fade time
    private void Fade(AudioSource source, float target, float step)
    {
        if (source.clip != null)
            source.volume = Mathf.MoveTowards(source.volume, target, step * volume);
    }
}
