using UnityEngine;

[CreateAssetMenu(menuName = "Pickups/WeaponPickup")]
public class WeaponPickupSO : ScriptableObject
{
    public GameObject weaponPrefab;     // Optional: prefab to spawn when picked up
    public string displayName;          // Optional: name for UI
}
