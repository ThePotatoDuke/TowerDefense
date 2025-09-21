using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/ProjectileData")]
public class ProjectileDataSO : ScriptableObject
{
    [Header("General")]
    public float speed = 10f;
    public float damage = 5f;
    public float lifetime = 5f;

    [Header("Special")]
    public bool pierces = false;
    public int maxPierceCount = 0;
}
