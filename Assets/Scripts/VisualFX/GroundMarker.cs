using DG.Tweening;
using UnityEngine;

// Target marker lying on the ground: a spinning outer ring with a dot in the middle that scales up when
// shown and down when hidden. Warns where something is about to land, like an enemy dropping from the sky
// (SkyDrop) or a shot (EnemyBullet). Can show several at once in a line, e.g. along a roller. Sprites and
// timing are under Sky Drop in FXSettings.
public class GroundMarker : MonoBehaviour
{
    // Below the sprites standing on the ground (they're at 0 and up), so it never covers one
    private const int SortingOrder = -20;
    // A little above the ground, so a road that banks slightly (a tilted knot) doesn't poke through it
    private const float Lift = 0.1f;

    private readonly System.Collections.Generic.List<Transform> outerRings = new();
    private Material material;

    // offsets places a marker at each offset from the point (world units); without it there's one on it
    public static GroundMarker Show(Vector3 groundPoint, Vector3[] offsets = null)
    {
        var markerObject = new GameObject("GroundMarker");
        markerObject.transform.position = groundPoint + Vector3.up * Lift;
        var marker = markerObject.AddComponent<GroundMarker>();
        marker.Build(offsets != null && offsets.Length > 0 ? offsets : new[] { Vector3.zero });
        return marker;
    }

    // Shrinks away and is gone
    public void Hide()
    {
        transform.DOKill();
        transform.DOScale(0f, 0.15f).SetEase(Ease.InBack).SetLink(gameObject).OnComplete(() => Destroy(gameObject));
    }

    private void Build(Vector3[] offsets)
    {
        FXSettings settings = FXSettings.Instance;
        if (settings != null && settings.spriteFXShader != null)
            material = new Material(settings.spriteFXShader) { name = "GroundMarker (Instance)" };

        for (int i = 0; i < offsets.Length; i++)
        {
            var spot = new GameObject("Marker " + i).transform;
            spot.SetParent(transform, false);
            spot.localPosition = offsets[i];
            outerRings.Add(CreateLayer(spot, "Outer", settings != null ? settings.dropMarkerOuter : null, SortingOrder));
            CreateLayer(spot, "Middle", settings != null ? settings.dropMarkerMiddle : null, SortingOrder + 1);
        }

        transform.localScale = Vector3.zero;
        transform.DOScale(1f, settings != null ? settings.dropMarkerPopTime : 0.25f).SetEase(Ease.OutBack).SetLink(gameObject);
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
