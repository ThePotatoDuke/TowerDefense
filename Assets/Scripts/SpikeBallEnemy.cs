using UnityEngine;

public class SpikeBallEnemy : EnemyBase
{
    [SerializeField] private SpikeBallDataSO data;
    [SerializeField] private Transform spikeBallVisual;

    public override EnemyDataSO Data => data;

    private float velocityX = 0f;   // current X velocity
    private float targetX;          // where we want to go

    private void Awake()
    {
        targetX = 0; // initial target
    }

    private void Update()
    {
        targetX = data.targetX;

        float dt = Time.deltaTime;

        // Hooke's law: F = -k * x
        float displacement = transform.position.x - targetX;
        float force = -data.springK * displacement;

        // Apply damping
        float damping = -data.damping * velocityX;

        // Acceleration
        float acceleration = force + damping;

        // Update velocity and position
        velocityX += acceleration * dt;
        velocityX = Mathf.Clamp(velocityX, -data.maxSpeed, data.maxSpeed);

        Vector3 pos = transform.position;
        pos.x += velocityX * dt;
        transform.position = pos;

        //rotate the ball
        float rotationAmount = (velocityX * dt / (2 * Mathf.PI * data.ballRadius)) * 360f;
        spikeBallVisual.Rotate(Vector3.forward, -rotationAmount, Space.Self);

    }

    // Optional: call this to move target somewhere else
    public void SetTargetX(float newTarget)
    {
        targetX = newTarget;
    }
}
