using System;
using DG.Tweening;
using UnityEngine;

// Carrying a Crimson Weed seed bought in the shop: a faint weed and its dashed range circle follow the
// player's feet, green where it can be planted (beside the path, not on it, away from the castle and from
// other weeds) and red where it can't. E plants it there. One seed is carried at a time. Lives on the
// player; the planting rules are on the Crimson Weed prefab.
public class WeedPlanter : MonoBehaviour
{
    public static WeedPlanter Instance { get; private set; }

    private const float PreviewAlpha = 0.5f;

    private Player player;
    private Transform preview;
    private CrimsonWeed ghost; // the weed shown at the feet, switched off; its numbers are the prefab's
    private SpriteRenderer previewSprite;
    private RangeCircle circle;
    private bool canPlant;

    public bool HasSeed => preview != null;

    // Seed bought or planted
    public event Action OnChanged;

    public void Setup(Player owner)
    {
        Instance = this;
        player = owner;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        if (preview != null)
            Destroy(preview.gameObject);
        if (circle != null)
            Destroy(circle.gameObject);
    }

    // Bought in the shop
    public void GiveSeed()
    {
        if (HasSeed) return;

        // The weed itself, switched off: a ghost that doesn't shoot or cast a shadow
        ghost = CrimsonWeed.Spawn(FeetPosition());
        if (ghost == null) return;
        ghost.enabled = false;
        if (ghost.TryGetComponent(out BlobShadow shadow))
            Destroy(shadow);
        ghost.name = "Crimson Weed Preview";
        preview = ghost.transform;
        previewSprite = ghost.GetComponentInChildren<SpriteRenderer>();

        circle = RangeCircle.Create();
        circle.SetRadius(ghost.Range);
        Refresh();
        OnChanged?.Invoke();
    }

    // E pressed
    public void TryPlant()
    {
        if (!HasSeed || player == null || player.IsDead() || GameStateManager.CurrentState != GameState.Playing) return;

        Refresh();
        if (!canPlant)
        {
            // Not here: a little shake of the ghost says so
            Transform body = previewSprite.transform;
            body.DOKill(true);
            body.DOShakePosition(0.25f, new Vector3(0.12f, 0f, 0f), 20, 0f).SetLink(body.gameObject);
            return;
        }

        CrimsonWeed.Plant(preview.position);
        Destroy(preview.gameObject);
        Destroy(circle.gameObject);
        preview = null;
        OnChanged?.Invoke();
    }

    private void LateUpdate()
    {
        if (HasSeed)
            Refresh();
    }

    private void Refresh()
    {
        Vector3 point = FeetPosition();
        preview.position = point;
        circle.Place(point);
        canPlant = CanPlantAt(point);

        Color color = canPlant ? ghost.ValidColor : ghost.InvalidColor;
        circle.SetColor(color);
        previewSprite.color = new Color(color.r, color.g, color.b, PreviewAlpha);
    }

    private Vector3 FeetPosition() => CastleGround.OnGround(player != null ? player.transform.position : transform.position);

    private bool CanPlantAt(Vector3 point)
    {
        float fromPath = CastleGround.DistanceToPath(point);
        if (fromPath < ghost.MinPathDistance || fromPath > ghost.MaxPathDistance) return false;

        Vector3 fromCastle = point - CastleGround.End;
        fromCastle.y = 0f;
        if (fromCastle.magnitude < ghost.CastleClearance) return false;

        foreach (CrimsonWeed weed in CrimsonWeed.All)
        {
            Vector3 offset = weed.transform.position - point;
            offset.y = 0f;
            if (offset.magnitude < ghost.Spacing) return false;
        }
        return true;
    }
}
