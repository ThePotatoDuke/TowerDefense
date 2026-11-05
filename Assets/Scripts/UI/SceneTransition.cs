using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Changes scene behind a screen of pixel diamonds: they grow in over everything (matching the background
// they're copied from, so it looks like the background swallowing the menu), the scene loads behind them,
// then they shrink away to show the new one. Lives on its own canvas over every other one that survives
// the load, and blocks clicks while it's up.
public class SceneTransition : MonoBehaviour
{
    private static SceneTransition current;

    private ScrollingBackground diamonds;

    public static bool Busy => current != null;

    // look: a background to copy the diamonds' size, colours and scroll from (or the defaults without one).
    // beforeLoad runs once the screen is covered, just before the scene changes (unpausing, resetting).
    public static void Load(string sceneName, float duration, ScrollingBackground look = null, Action beforeLoad = null)
    {
        if (current != null) return;
        Sounds.Play(Sound.MenuTransition);

        var canvasObject = new GameObject("Scene Transition", typeof(RectTransform));
        DontDestroyOnLoad(canvasObject);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;
        canvasObject.AddComponent<GraphicRaycaster>();

        var screen = new GameObject("Diamonds", typeof(RectTransform), typeof(RawImage)).GetComponent<RectTransform>();
        screen.SetParent(canvasObject.transform, false);
        screen.anchorMin = Vector2.zero;
        screen.anchorMax = Vector2.one;
        screen.offsetMin = screen.offsetMax = Vector2.zero;

        current = canvasObject.AddComponent<SceneTransition>();
        current.diamonds = screen.gameObject.AddComponent<ScrollingBackground>();
        if (look != null)
            current.diamonds.CopyFrom(look);
        current.diamonds.Reveal = 0f;
        current.StartCoroutine(current.Run(sceneName, duration, beforeLoad));
    }

    private IEnumerator Run(string sceneName, float duration, Action beforeLoad)
    {
        yield return diamonds.PlayReveal(true, duration).WaitForCompletion();
        beforeLoad?.Invoke();

        AsyncOperation loading = SceneManager.LoadSceneAsync(sceneName);
        while (loading != null && !loading.isDone)
            yield return null;
        // A couple of frames for the new scene's first, slow ones to pass, so the reveal doesn't skip
        yield return null;
        yield return null;

        yield return diamonds.PlayReveal(false, duration).WaitForCompletion();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (current == this)
            current = null;
    }
}
