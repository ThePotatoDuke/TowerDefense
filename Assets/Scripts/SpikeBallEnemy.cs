using UnityEngine;

public class SpikeBallEnemy : EnemyBase
{
    [SerializeField] private SpikeBallDataSO data;
    [SerializeField] private Transform spikeBallVisual;
    private Vector3 lastPos;


    public override EnemyDataSO Data => data;

    private float velocityX = 0f;   // Current X velocity
    private float targetX;          // Where we want to go

    private void Awake()
    {
        base.Awake();
        targetX = 0; // Initial target
    }
    private void Update()
    {
        float dt = Time.deltaTime;

        // --- Movement logic ---
        Vector3 currentPos = transform.position;

        // Example: move toward targetX and targetZ (you can add targetZ to your data)
        Vector3 targetPos = new Vector3(data.targetX, currentPos.y, currentPos.z);
        Vector3 displacementVec = currentPos - targetPos;

        // Hooke's law: F = -k * x
        Vector3 force = -data.springK * displacementVec;

        // Damping: F = -b * v
        Vector3 damping = -data.damping * new Vector3(velocityX, 0f, 0f); // can extend to Z if using velocityZ

        // Acceleration
        Vector3 acceleration = force + damping;

        // Update velocity (only horizontal plane)
        velocityX += acceleration.x * dt;
        velocityX = Mathf.Clamp(velocityX, -data.maxSpeed, data.maxSpeed);

        // Move on X axis
        currentPos.x += velocityX * dt;

        // For Z movement, add similar velocityZ if needed
        // currentPos.z += velocityZ * dt;

        transform.position = currentPos;

        // --- Rolling visual on plane ---
        Vector3 movement = transform.position - lastPos;
        float distanceTraveled = movement.magnitude;

        if (distanceTraveled > 0.0001f)
        {
            // Rolling axis perpendicular to movement and plane normal
            Vector3 rollingAxis = Vector3.Cross(Vector3.up, movement.normalized);

            float rotationDegrees = (distanceTraveled / (2 * Mathf.PI * data.ballRadius)) * 360f;

            spikeBallVisual.Rotate(rollingAxis, rotationDegrees, Space.World);
        }

        lastPos = transform.position;
    }


    // Optional: set a new target X
    public void SetTargetX(float newTarget)
    {
        targetX = newTarget;
    }
}
