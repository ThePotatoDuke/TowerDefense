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
    public void Init(ProjectileDataSO data, Vector3 direction)
    {
        this.data = data;
        this.direction = direction.normalized;
        pierceCount = 0;

        Destroy(gameObject, data.lifetime);
    }

    [SerializeField] Transform bulletVisual;

    public void SetDirection(Vector3 dir)
    {
        if (bulletVisual == null) return;

        // Rotate only around local Y
        float yAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        bulletVisual.localEulerAngles = new Vector3(0f, yAngle, 0f);
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
