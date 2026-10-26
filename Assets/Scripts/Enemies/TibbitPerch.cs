using UnityEngine;

// Marks where a tibbit sits when it rides this enemy. Put it on a child of the enemy's sprite, so it
// follows the sprite when it flips, hops or squashes, and move it onto the head. Enemies without one get
// a tibbit on the top middle of their sprite. Tools > Add Tibbit Perches adds one to every enemy prefab.
public class TibbitPerch : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        // A small marker with a line down, so it's easy to line up on the head
        Gizmos.color = new Color(0.4f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, 0.06f);
        Gizmos.DrawLine(transform.position, transform.position + transform.up * 0.3f);
    }
}
