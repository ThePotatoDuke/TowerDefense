using UnityEngine;

public class Projectile : MonoBehaviour
{
    private ProjectileDataSO data;
    private Vector3 direction;
    private int pierceCount;

    private void Awake()
    {
        // Automatically find the child named "BulletVisual"
        bulletVisual = transform.Find("BulletVisual");
        if (bulletVisual == null)
            Debug.LogWarning("BulletVisual child not found!");
    }
    public void Initialize(ProjectileDataSO data, Vector3 direction)
    {
        this.data = data;
        this.direction = direction.normalized;
        pierceCount = 0;
        SetDirection();
        Destroy(gameObject, data.lifetime);

    }

    [SerializeField] Transform bulletVisual;

    public void SetDirection()
    {
        // root points along travel direction
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        // optional: add an extra Y spin to the child (e.g., aim sword/gun)
        if (bulletVisual != null)
        {
            float yAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            bulletVisual.localEulerAngles += new Vector3(0f, yAngle, 0f);
        }
    }




    private void Update()
    {
        transform.position += direction * data.speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IHasHealth>(out var target))
        {
            target.TakeDamage(data.damage);

            if (!data.pierces || pierceCount >= data.maxPierceCount)
            {
                Destroy(gameObject);
            }
            else
            {
                pierceCount++;
            }
        }
    }
}
