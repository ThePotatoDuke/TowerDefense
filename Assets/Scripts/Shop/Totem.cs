using UnityEngine;

// The Totem standing by the castle, and (on its prefab, Prefabs/Shop/Totem, which ShopSettings points to)
// where it stands and how long the warp takes to channel. TotemAbility places it and uses it.
public class Totem : MonoBehaviour
{
    [Tooltip("Where it stands: x = how far back along the path from the castle, y = how far to the side.")]
    [SerializeField] private Vector2 spot = new(2.6f, 3f);
    [Tooltip("Seconds T has to be held before the warp; letting go or getting hurt cancels it " +
             "(the totem is kept).")]
    [SerializeField] private float channelTime = 0.8f;

    public Vector2 Spot => spot;
    public float ChannelTime => channelTime;
}
