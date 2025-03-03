using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
public class TennisBall : MonoBehaviour
{
    [Header("Physics Properties")]
    [SerializeField] private float mass = 0.057f; // 57g
    [SerializeField] private float drag = 0.5f;
    [SerializeField] private float angularDrag = 0.05f;
    [SerializeField] private float bounciness = 0.8f;
    [SerializeField] private float gravityCorrectionFactor = 1.1f;

    [Header("Spin Effects")]
    [SerializeField] private float spinEffectStrength = 0.5f;
    [SerializeField] private float spinDecayRate = 0.98f;

    [Header("Court Effects")]
    [SerializeField] private float grassCourtSpeedFactor = 1.2f;
    [SerializeField] private float clayCourtSpeedFactor = 0.85f;
    [SerializeField] private float hardCourtBounceFactor = 1.0f;

    [Header("Components")]
    [SerializeField] private TrailRenderer trail;

    // State
    private Vector3 spinVector;
    private CourtType currentCourtType = CourtType.Hard;
    private BallState currentState = BallState.Idle;

    // Properties
    public bool LastHitByPlayer { get; private set; }
    public Vector3 LastBouncePosition { get; private set; }
    
    // References
    private Rigidbody rb;
    
    // Events
    public event Action<Vector3> OnBounce;
    public event Action<bool> OnHit; // true if hit by player

    public enum CourtType { Grass, Clay, Hard, Indoor }
    public enum BallState { Idle, InPlay, OutOfBounds }

    private void Awake()
    {
        // Get references if not assigned
        rb = GetComponent<Rigidbody>();
        if (trail == null) trail = GetComponent<TrailRenderer>();
        
        // Configure physics properties
        rb.mass = mass;
        rb.drag = drag;
        rb.angularDrag = angularDrag;
        
        // Initially disable trail
        if (trail != null)
        {
            trail.emitting = false;
        }
    }

    private void FixedUpdate()
    {
        if (currentState == BallState.InPlay)
        {
            // Apply additional gravity for more realistic trajectory
            rb.AddForce(Physics.gravity * gravityCorrectionFactor, ForceMode.Acceleration);
            
            // Apply spin effects
            ApplySpinEffects();
            
            // Gradually reduce spin
            spinVector *= spinDecayRate;
            
            // Check if ball has stopped (prevent endless rolling)
            if (rb.velocity.magnitude < 0.5f && transform.position.y < 0.1f)
            {
                StartCoroutine(DelayedResetIfStopped(1.0f));
            }
        }
    }

    public void Hit(Vector3 direction, float power, float spinFactor, bool hitByPlayer = true)
    {
        // Update hit information
        LastHitByPlayer = hitByPlayer;
        
        // Reset state
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        // Calculate base force with court effects
        float courtSpeedMultiplier = GetCourtSpeedMultiplier();
        Vector3 hitForce = direction.normalized * power * courtSpeedMultiplier;
        
        // Apply vertical adjustment based on spin
        hitForce.y += spinFactor * power * 0.2f;
        
        // Set spin vector based on direction and spin factor
        spinVector = CalculateSpinVector(direction, spinFactor);
        
        // Apply force
        rb.AddForce(hitForce, ForceMode.Impulse);
        
        // Add torque for visual spinning
        rb.AddTorque(spinVector * 20f, ForceMode.Impulse);
        
        // Update state
        currentState = BallState.InPlay;
        
        // Enable trail
        if (trail != null)
        {
            trail.emitting = true;
        }
        
        // Trigger hit event
        OnHit?.Invoke(hitByPlayer);
    }

    private void ApplySpinEffects()
    {
        if (spinVector.magnitude < 0.1f) return;
        
        // Calculate spin force based on current velocity and spin vector
        Vector3 spinForce = Vector3.Cross(rb.velocity, spinVector) * spinEffectStrength;
        
        // Apply the spin force
        rb.AddForce(spinForce, ForceMode.Force);
    }

    private Vector3 CalculateSpinVector(Vector3 direction, float spinFactor)
    {
        // Topspin: rotates around X axis (perpendicular to direction)
        // Backspin: negative rotation around X axis
        // Sidespin: rotates around Y axis
        
        Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
        
        // Primarily topspin or backspin (around right vector)
        return right * spinFactor * 10f;
    }

    private float GetCourtSpeedMultiplier()
    {
        switch (currentCourtType)
        {
            case CourtType.Grass: return grassCourtSpeedFactor;
            case CourtType.Clay: return clayCourtSpeedFactor;
            default: return 1.0f;
        }
    }

    public void SetCourtType(CourtType courtType)
    {
        currentCourtType = courtType;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Handle bounce effects
        if (collision.gameObject.CompareTag("Court"))
        {
            // Record bounce position
            LastBouncePosition = transform.position;
            
            // Apply court-specific bounce effects
            if (currentCourtType == CourtType.Clay)
            {
                // Reduce velocity more on clay
                rb.velocity *= 0.9f;
            }
            
            // Trigger bounce event
            OnBounce?.Invoke(LastBouncePosition);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (currentState != BallState.InPlay) return;
        
        if (other.CompareTag("OutOfBounds"))
        {
            currentState = BallState.OutOfBounds;
            // Notify game manager through singleton
            GameManager.Instance?.ProcessOutOfBounds(transform.position);
        }
        else if (other.CompareTag("Net"))
        {
            // Notify game manager of net hit
            GameManager.Instance?.ProcessNetHit();
        }
    }

    public void Reset()
    {
        // Reset ball state for new point
        currentState = BallState.Idle;
        spinVector = Vector3.zero;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        // Reset position data
        LastBouncePosition = Vector3.zero;
        
        if (trail != null)
        {
            trail.emitting = false;
            trail.Clear();
        }
        
        // Cancel any pending coroutines
        StopAllCoroutines();
    }
    
    private System.Collections.IEnumerator DelayedResetIfStopped(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Check if ball is still moving very slowly
        if (rb.velocity.magnitude < 0.2f && transform.position.y < 0.1f)
        {
            // Notify game manager that the ball has stopped
            GameManager.Instance?.ProcessBallStopped(transform.position, LastHitByPlayer);
        }
    }
}
