using UnityEngine;

public class SpikeBallEnemy : EnemyBase
{
    [SerializeField] private SpikeBallDataSO data;
    [SerializeField] private Transform spikeBallVisual;
    private Vector3 lastPos;


    public override EnemyDataSO Data => data;

    private float progressVelocity = 0f;   // Current X velocity


    private void Awake()
    {
        base.Awake();

    }
    private float progress = 0f;        // your path progress

    private void Update()
    {
        float dt = Time.deltaTime;

        // --- Movement along the path ---
        float targetProgress = data.targetProgress;
        float displacement = progress - targetProgress;

        // Hooke's law: F = -k * x
        float force = -data.springK * displacement;

        // Damping: F = -b * v
        float damping = -data.damping * progressVelocity;

        // Acceleration
        float acceleration = force + damping;

        // Update velocity
        progressVelocity += acceleration * dt;
        progressVelocity = Mathf.Clamp(progressVelocity, -data.maxSpeed, data.maxSpeed);

        // Update progress
        float previousProgress = progress;
        progress += progressVelocity * dt;

        // --- Convert progress to actual 3D position ---
        // For now, let's assume linear along X for testing
        Vector3 newPos = transform.position;
        newPos.x += progress - previousProgress;   // delta movement along X
        transform.position = newPos;

        // --- Rolling visual ---
        Vector3 movement = transform.position - lastPos;
        float distanceTraveled = movement.magnitude;

        if (distanceTraveled > 0.0001f)
        {
            Vector3 rollingAxis = Vector3.Cross(Vector3.up, movement.normalized);
            float rotationDegrees = (distanceTraveled / (2 * Mathf.PI * data.ballRadius)) * 360f;
            spikeBallVisual.Rotate(rollingAxis, rotationDegrees, Space.World);
        }

        lastPos = transform.position;
    }
}





