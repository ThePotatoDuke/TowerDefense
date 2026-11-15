using UnityEngine;

// Whether the music and the sound effects are on, as set by the toggles on the pause menu. Kept between
// runs (PlayerPrefs). MusicManager and the menu's music go quiet while music's off; Sounds plays nothing
// while sound effects are off.
public static class AudioToggles
{
    private const string MusicKey = "MusicOn";
    private const string SfxKey = "SfxOn";

    private static bool loaded;
    private static bool musicOn = true;
    private static bool sfxOn = true;

    public static bool MusicOn
    {
        get { Load(); return musicOn; }
        set { Load(); musicOn = value; Save(MusicKey, value); }
    }

    public static bool SfxOn
    {
        get { Load(); return sfxOn; }
        set { Load(); sfxOn = value; Save(SfxKey, value); }
    }

    private static void Load()
    {
        if (loaded) return;
        loaded = true;
        musicOn = PlayerPrefs.GetInt(MusicKey, 1) == 1;
        sfxOn = PlayerPrefs.GetInt(SfxKey, 1) == 1;
    }

    private static void Save(string key, bool on)
    {
        PlayerPrefs.SetInt(key, on ? 1 : 0);
        PlayerPrefs.Save();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize() => loaded = false; // read again each play session (domain reload may be off)
}
