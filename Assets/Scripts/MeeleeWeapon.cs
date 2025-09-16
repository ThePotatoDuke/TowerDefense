using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MeleeWeapon : MonoBehaviour
{
    private HashSet<GameObject> alreadyHitEnemies = new HashSet<GameObject>();
    private List<GameObject> enemiesInRange = new List<GameObject>();

    [SerializeField] private MeleeWeaponDataSO data;
    // [SerializeField] private Collider weaponCollider;
    [SerializeField] private TrailRenderer trail;

    [HideInInspector] public GameObject owner;

    private float lastAttackTime = 0f;

    private void OnEnable()
    {
        StartCoroutine(AutoAttackRoutine());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator AutoAttackRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f); // check every 0.1s
            GameObject target = FindClosestEnemy();
            if (target != null)
                Attack(target);
        }
    }


    private GameObject FindClosestEnemy()
    {
        GameObject closestEnemy = null;
        float closestDistance = float.MaxValue;

        // Iterate through the pre-filtered list
        foreach (var enemy in enemiesInRange)
        {
            if (enemy == null) continue; // Handle destroyed enemies

            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestEnemy = enemy;
            }
        }
        return closestEnemy;
    }

    private void Attack(GameObject target)
    {
        float cooldown = 1f / data.attackRate;
        if (Time.time < lastAttackTime + cooldown) return;

        lastAttackTime = Time.time;
        StartCoroutine(SwingCoroutine(target));
    }

    private IEnumerator SwingCoroutine(GameObject target)
    {
        // Clear the set of enemies hit by this swing to prevent multiple hits
        alreadyHitEnemies.Clear();

        // Enable the visual trail effect for the weapon
        if (trail != null)
            trail.enabled = true;

        // Calculate the direction to the target, ignoring any vertical difference (y-axis)
        Vector3 dirToTarget = (target.transform.position - owner.transform.position);
        dirToTarget.y = 0;

        if (dirToTarget == Vector3.zero) // Handle edge case where target is at the same position
        {
            yield break; // Exit the coroutine
        }

        dirToTarget.Normalize();

        // Determine the starting and ending rotations for the swing.
        // The swing rotates from -60 degrees to +60 degrees relative to the target's direction.
        Quaternion startRotation = Quaternion.LookRotation(dirToTarget) * Quaternion.Euler(0, -60f, 0);
        Quaternion endRotation = Quaternion.LookRotation(dirToTarget) * Quaternion.Euler(0, 60f, 0);

        float elapsed = 0f;
        float hitStart = data.swingDuration * 0.3f;
        float hitEnd = data.swingDuration * 0.6f;

        while (elapsed < data.swingDuration)
        {
            // Calculate the interpolation value (t) from 0 to 1 over the swing duration
            float t = elapsed / data.swingDuration;

            // Smoothly rotate the weapon from the starting to ending rotation
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);

            // Activate the weapon's collider during the hit window to register damage
            // weaponCollider.enabled = (elapsed >= hitStart && elapsed <= hitEnd);

            // Wait until the next frame
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure the swing finishes at the correct end rotation
        transform.rotation = endRotation;

        // Disable the collider and trail effect after the swing is complete
        // weaponCollider.enabled = false;
        if (trail != null)
            trail.enabled = false;
    }



    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == owner) return;

        // Check if the entering object is an enemy and add it to the list
        if (other.TryGetComponent<EnemyBase>(out var enemy))
        {
            enemiesInRange.Add(enemy.gameObject);
        }
        if (other.TryGetComponent<IHasHealth>(out var target))
        {
            // Check if we've already hit this enemy in the current swing
            if (alreadyHitEnemies.Add(other.gameObject))
            {
                target.TakeDamage(data.damage);
                // ... (apply knockback logic here)
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        // Check if the leaving object is an enemy and remove it from the list
        if (other.TryGetComponent<EnemyBase>(out var enemy))
        {
            enemiesInRange.Remove(enemy.gameObject);
        }
    }

}
