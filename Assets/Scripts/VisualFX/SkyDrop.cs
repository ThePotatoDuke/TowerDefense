using System.Collections;
using DG.Tweening;
using UnityEngine;

// Drops an enemy from the sky onto a spot on the path, for enemies that would clip through the bushes
// coming in at the start (rollers, spike balls). A marker lying on the ground scales up first, its outer
// ring turning, as a warning (several in a line for something long, like a roller); the enemy falls onto
// it at the end of the warning, the camera shakes, and the enemy starts moving from there. Settings are
// under Sky Drop in FXSettings: the marker shows for Drop Warning Time, the last Drop Fall Time of which
// is the fall.
public class SkyDrop : MonoBehaviour
{
    private EnemyBase enemy;
    private Vector3 ground;
    private readonly System.Collections.Generic.List<Transform> outerRings = new();
    private Vector3[] spots;
    private Material material;

    // The enemy must already be on the path at the spot (EnemyBase.PlaceOnPath) and switched off; it's
    // switched back on when it lands. markerOffsets places a marker at each offset from the spot (world
    // units); without it there's one marker on the spot.
    public static void Begin(EnemyBase enemy, Vector3 groundPoint, Vector3[] markerOffsets = null)
    {
        var marker = new GameObject("SkyDropMarker");
        marker.transform.position = groundPoint + Vector3.up * 0.03f; // just above the ground
        var drop = marker.AddComponent<SkyDrop>();
        drop.enemy = enemy;
        drop.ground = groundPoint;
        drop.spots = markerOffsets != null && markerOffsets.Length > 0 ? markerOffsets : new[] { Vector3.zero };
        drop.Build();
        drop.StartCoroutine(drop.Run());
    }

    private void Build()
    {
        FXSettings settings = FXSettings.Instance;
        if (settings != null && settings.spriteFXShader != null)
            material = new Material(settings.spriteFXShader) { name = "SkyDropMarker (Instance)" };

        for (int i = 0; i < spots.Length; i++)
        {
            var marker = new GameObject("Marker " + i).transform;
            marker.SetParent(transform, false);
            marker.localPosition = spots[i];
            outerRings.Add(CreateLayer(marker, "Outer", settings != null ? settings.dropMarkerOuter : null, 0));
            CreateLayer(marker, "Middle", settings != null ? settings.dropMarkerMiddle : null, 1);
        }
        transform.localScale = Vector3.zero;
    }

    // A sprite lying flat on the ground
    private Transform CreateLayer(Transform parent, string layerName, Sprite sprite, int order)
    {
        var layer = new GameObject(layerName);
        layer.transform.SetParent(parent, false);
        layer.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var renderer = layer.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        if (material != null)
            renderer.sharedMaterial = material;
        renderer.sortingOrder = order;
        return layer.transform;
    }

    private IEnumerator Run()
    {
        FXSettings settings = FXSettings.Instance;
        float warning = settings != null ? settings.dropWarningTime : 1.2f;
        float fallTime = settings != null ? Mathf.Min(settings.dropFallTime, warning) : 0.45f;
        float height = settings != null ? settings.dropHeight : 12f;

        transform.DOScale(1f, settings != null ? settings.dropMarkerPopTime : 0.25f).SetEase(Ease.OutBack).SetLink(gameObject);
        yield return new WaitForSeconds(warning - fallTime);

        if (enemy == null)
        {
            Destroy(gameObject);
            yield break;
        }

        // Falling from out of sight above the screen: visible, but not doing anything until it lands
        enemy.gameObject.SetActive(true);
        enemy.enabled = false;
        Vector3 top = ground + Vector3.up * HeightOutOfView(height);
        for (float time = 0f; time < fallTime && enemy != null; time += Time.deltaTime)
        {
            float t = time / fallTime;
            enemy.transform.position = Vector3.Lerp(top, ground, t * t); // speeding up as it falls
            yield return null;
        }

        if (enemy != null)
        {
            enemy.transform.position = ground;
            enemy.enabled = true;
            CameraShake.Shake(ground, settings != null ? settings.dropLandShake : 0.2f, 0.25f);
        }

        transform.DOKill();
        transform.DOScale(0f, 0.15f).SetEase(Ease.InBack).SetLink(gameObject).OnComplete(() => Destroy(gameObject));
    }

    // The lowest height (from the minimum, in steps) at which a point above the marker is off the top of
    // the camera's view, with a margin so the whole enemy starts out of sight
    private float HeightOutOfView(float minimum)
    {
        Camera view = Camera.main;
        if (view == null) return minimum;

        const float Margin = 0.15f; // of the screen's height
        const float Step = 1f;
        const float Highest = 200f;
        for (float height = minimum; height < Highest; height += Step)
        {
            Vector3 onScreen = view.WorldToViewportPoint(ground + Vector3.up * height);
            if (onScreen.z <= 0f || onScreen.y > 1f + Margin)
                return height;
        }
        return Highest;
    }

    private void Update()
    {
        float spin = (FXSettings.Instance != null ? FXSettings.Instance.dropMarkerSpin : 120f) * Time.deltaTime;
        foreach (Transform ring in outerRings)
            ring.Rotate(0f, 0f, spin, Space.Self);
    }

    private void OnDestroy()
    {
        if (material != null)
            Destroy(material);
    }
}
