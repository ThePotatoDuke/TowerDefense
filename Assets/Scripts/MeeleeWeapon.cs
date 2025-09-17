using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening; // You'll need this for the LINQ query

public class MeleeWeapon : MonoBehaviour
{
    private HashSet<GameObject> alreadyHitEnemies = new HashSet<GameObject>();

    [SerializeField] private MeleeWeaponDataSO data;
    [SerializeField] private TrailRenderer trail;
    [SerializeField] public GameObject owner;
    [SerializeField] public Transform playerHand;


    // A variable to store a reference to the Player_Body for checking distance
    private Transform playerBodyTransform;

    // The range within which the weapon will continuously look for targets
    [SerializeField] private float autoAttackRange = 5f;

    private float lastAttackTime = 0f;

    private void Awake()
    {
        // Get the Player_Body transform to calculate distance from
        playerBodyTransform = owner.transform;
    }

    private void OnEnable()
    {
        // Start the continuous attack routine when the script is enabled
        // StartCoroutine(AutoAttackRoutine());
    }

    void Update()
    {
        RotateSwordTowardsClosestEnemy();
    }
    private void OnDisable()
    {
        StopAllCoroutines();
    }

    // This routine now actively searches for a target within a radius
    private IEnumerator AutoAttackRoutine()
    {
        while (true)
        {
            // Wait for a small interval before checking again
            yield return new WaitForSeconds(0.1f);

            // --- The key change is here: Find the closest enemy in the world ---
            GameObject target = FindClosestEnemyInWorld();

            if (target != null)
            {
                Attack(target);
            }
        }
    }

    private GameObject FindClosestEnemyInWorld()
    {
        // Use a non-allocating method for performance
        Collider[] colliders = Physics.OverlapSphere(playerBodyTransform.position, autoAttackRange);

        GameObject closestEnemy = null;
        float closestDistance = float.MaxValue;

        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent<EnemyBase>(out var enemy))
            {
                // Check that the object is not the player
                if (collider.gameObject == owner) continue;

                float dist = Vector3.Distance(playerBodyTransform.position, collider.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestEnemy = collider.gameObject;
                }
            }
        }
        return closestEnemy;
    }

    private void Attack(GameObject target)
    {
        float cooldown = 1f / data.attackRate;
        if (Time.time < lastAttackTime + cooldown) return;

        lastAttackTime = Time.time;

        SwingWithDoTween(target); // call DoTween swing directly
    }
    private void RotateSwordTowardsClosestEnemy()
    {
        GameObject closestEnemy = FindClosestEnemyInWorld();
        if (closestEnemy == null) return;

        // Direction from hand to enemy
        Vector3 direction = closestEnemy.transform.position - playerHand.position;

        // Ignore vertical
        direction.y = 0;

        if (direction.sqrMagnitude < 0.0001f) return; // avoid zero-length

        // Calculate angle in XZ plane
        float angle = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;

        // Compensate for sprite rotated 90° on X
        float correctedZ = angle - 90f;

        // Apply rotation to the hand
        playerHand.localRotation = Quaternion.Euler(90, 0, correctedZ);
    }








    private void SwingWithDoTween(GameObject target)
    {
        if (trail != null)
            trail.enabled = true;

        // Direction from hand to target, ignoring height
        Vector3 dir = target.transform.position - playerHand.position;
        dir.y = 0; // ignore vertical
        if (dir == Vector3.zero) return;

        // Calculate angle in XZ plane
        float angle = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;

        // Our sprite is rotated 90° in X, so we swing in Z
        float swingHalfArc = 60f; // degrees

        float startAngle = angle - swingHalfArc;
        float endAngle = angle + swingHalfArc;

        // Set start rotation
        playerHand.localRotation = Quaternion.Euler(90, 0, startAngle);

        // Tween to end rotation
        playerHand.DOLocalRotateQuaternion(Quaternion.Euler(90, 0, endAngle), data.swingDuration)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                if (trail != null)
                    trail.enabled = false;
            });
    }




    // Keep these for damage registration
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IHasHealth>(out var target) && alreadyHitEnemies.Add(other.gameObject))
        {
            target.TakeDamage(data.damage);
        }
    }
}