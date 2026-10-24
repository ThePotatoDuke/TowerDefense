using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    public WeaponPickupSO weaponSO; // Assign in inspector

    private void Awake()
    {
        // Light sweep and ground shadow so drops catch the eye
        gameObject.GetOrAddComponent<SpriteFX>().SetShine(true);
        gameObject.GetOrAddComponent<BlobShadow>();
    }
}
