using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Plays the sound effects in the SoundLibrary. Sounds.Play is for ones heard the same anywhere (the UI, the
// player's own); Sounds.PlayAt is for something in the world, heard as the player would: full volume close
// by, fading away to nothing with distance, and panned to the side it's on. They play on in real time,
// through the shop's pause and scene changes, from a few shared sources that live for the whole game.
// Every button in every scene clicks. A scene with no audio listener of its own (the main menu) is heard
// through one kept on the sounds' object, switched off whenever the scene has one (the game's camera).
public static class Sounds
{
    private const int SourceCount = 16;

    private static Transform root;
    private static AudioListener listener;
    private static AudioSource[] sources;
    private static int next;
    private static readonly Dictionary<Sound, float> lastPlayed = new();

    public static void Play(Sound sound, float pitch = 1f) => Play(sound, pitch, 1f, 0f);

    // Heard from where the player is (the camera, without one)
    public static void PlayAt(Sound sound, Vector3 position, float pitch = 1f)
    {
        SoundLibrary library = SoundLibrary.Instance;
        if (library == null) return;

        Camera view = Camera.main;
        Vector3 ears = Player.Instance != null ? Player.Instance.transform.position
                     : view != null ? view.transform.position : position;
        Vector3 offset = position - ears;
        offset.y = 0f;

        float distance = offset.magnitude;
        float loudness = 1f - Mathf.InverseLerp(library.fullVolumeDistance, library.silentDistance, distance);
        if (loudness <= 0.01f) return;
        loudness *= loudness; // falls away quickly at first, like real sound

        float side = view != null ? Vector3.Dot(offset, view.transform.right) : offset.x;
        float pan = Mathf.Clamp(side / Mathf.Max(0.01f, library.silentDistance * 0.5f), -1f, 1f) * library.stereoWidth;
        Play(sound, pitch, loudness, pan);
    }

    private static void Play(Sound sound, float pitch, float loudness, float pan)
    {
        if (!AudioToggles.SfxOn) return;
        SoundLibrary library = SoundLibrary.Instance;
        SoundLibrary.Entry entry = library != null ? library.Get(sound) : null;
        if (entry == null || entry.clip == null) return;

        // Too soon after the last one: skipped
        float now = Time.unscaledTime;
        if (lastPlayed.TryGetValue(sound, out float last) && now - last < entry.minGap) return;
        lastPlayed[sound] = now;

        AudioSource source = NextSource();
        source.pitch = pitch * (1f + Random.Range(-entry.pitchVariation, entry.pitchVariation));
        source.panStereo = pan;
        source.PlayOneShot(entry.Pick(), entry.volume * library.masterVolume * loudness);
    }

    // A sound that repeats until stopped (Stop the source it returns), or null if it has no clip
    public static AudioSource PlayLoop(Sound sound)
    {
        SoundLibrary library = SoundLibrary.Instance;
        SoundLibrary.Entry entry = library != null ? library.Get(sound) : null;
        if (entry == null || entry.clip == null) return null;

        EnsureRoot();
        AudioSource source = root.gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.clip = entry.clip;
        source.loop = true;
        source.volume = entry.volume * library.masterVolume;
        source.mute = !AudioToggles.SfxOn;
        source.Play();
        return source;
    }

    public static void StopLoop(AudioSource loop)
    {
        if (loop != null)
            Object.Destroy(loop);
    }

    private static AudioSource NextSource()
    {
        EnsureRoot();
        AudioSource source = sources[next];
        next = (next + 1) % sources.Length;
        return source;
    }

    private static void EnsureRoot()
    {
        if (root != null) return;
        var gameObject = new GameObject("Sounds");
        Object.DontDestroyOnLoad(gameObject);
        root = gameObject.transform;
        listener = gameObject.AddComponent<AudioListener>();
        UpdateListener();
        sources = new AudioSource[SourceCount];
        for (int i = 0; i < SourceCount; i++)
        {
            sources[i] = gameObject.AddComponent<AudioSource>();
            sources[i].playOnAwake = false;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        // Static state survives play sessions when domain reload is off, so clear it explicitly
        root = null;
        listener = null;
        sources = null;
        next = 0;
        lastPlayed.Clear();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    // Every button in the scene clicks when pressed, except one that starts a scene change (Play, Retry,
    // Menu): that has the transition's sound instead. The click comes after the button's own action (set in
    // the scene), so the transition has already started by then.
    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureRoot();
        UpdateListener();

        foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include))
            if (button.gameObject.scene == scene)
                button.onClick.AddListener(() =>
                {
                    if (!SceneTransition.Busy)
                        Play(Sound.ButtonClick);
                });
    }

    // Ours is on only while nothing else listens (two at once makes Unity complain)
    private static void UpdateListener()
    {
        if (listener == null) return;
        bool another = false;
        foreach (AudioListener other in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude))
            if (other != listener && other.isActiveAndEnabled)
                another = true;
        listener.enabled = !another;
    }
}
