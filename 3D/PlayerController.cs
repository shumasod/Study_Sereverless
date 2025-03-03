using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float positionResetTime = 1.0f;

    [Header("Shot Settings")]
    [SerializeField] private float maxPowerCharge = 1.5f;
    [SerializeField] private float chargeRate = 1f;
    [SerializeField] private float minPower = 5f;
    [SerializeField] private float maxPower = 20f;
    [SerializeField] private float shotCooldown = 0.5f;

    [Header("Shot Types")]
    [SerializeField] private float topspinFactor = 0.8f;
    [SerializeField] private float backspinFactor = -0.5f;
    [SerializeField] private float flatFactor = 0.2f;
    [SerializeField] private float lobHeight = 10f;
    [SerializeField] private float smashPowerMultiplier = 1.8f;

    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform racketTransform;
    [SerializeField] private Transform reachAreaTransform;
    [SerializeField] private ParticleSystem hitParticleSystem;

    // Input values
    private Vector2 movementInput;
    private bool isSprinting;

    // State
    private bool isCharging = false;
    private float currentCharge = 0f;
    private TennisBall targetBall;
    private int selectedShotType = 0; // 0=Flat, 1=Topspin, 2=Backspin, 3=Lob, 4=Smash
    private bool canShoot = true;
    private bool isInServeMode = false;
    private bool hasServed = false;
    private Vector3 startPosition;
    private float chargeStartTime;

    // Animation parameter hashes
    private int speedHash;
    private int directionHash;
    private int shotTypeHash;
    private int hitTriggerHash;
    private int chargeHash;
    private int serveHash;

    // Components
    private CharacterController controller;

    private void Awake()
    {
        // Get references if not assigned
        controller = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        
        if (racketTransform == null)
        {
            // Try to find racket transform if not assigned
            Transform[] allTransforms = GetComponentsInChildren<Transform>();
            foreach (Transform t in allTransforms)
            {
                if (t.name.ToLower().Contains("racket"))
                {
                    racketTransform = t;
                    break;
                }
            }
            
            if (racketTransform == null)
            {
                Debug.LogWarning("Racket transform not assigned and not found in children!");
            }
        }

        // Cache animation parameter hashes
        speedHash = Animator.StringToHash("Speed");
        directionHash = Animator.StringToHash("Direction");
        shotTypeHash = Animator.StringToHash("ShotType");
        hitTriggerHash = Animator.StringToHash("Hit");
        chargeHash = Animator.StringToHash("ChargeAmount");
        serveHash = Animator.StringToHash("Serve");
        
        // Store starting position
        startPosition = transform.position;
    }

    private void Update()
    {
        if (isInServeMode)
        {
            // Serve mode - limited movement
            UpdateServeMode();
        }
        else
        {
            // Normal gameplay
            Move();
            UpdateShotCharge();
        }
        
        UpdateAnimations();
    }

    public void OnMove(Vector2 input)
    {
        movementInput = input;
    }

    public void OnSprint(bool sprint)
    {
        isSprinting = sprint;
    }

    public void OnChargeShotStart()
    {
        if (!canShoot || isInServeMode) return;
        
        isCharging = true;
        currentCharge = 0f;
        chargeStartTime = Time.time;
    }

    public void OnChargeShotRelease()
    {
        if (!isCharging) return;
        
        ExecuteShot();
        isCharging = false;
    }

    public void OnChangeShotType()
    {
        // Cycle through shot types
        selectedShotType = (selectedShotType + 1) % 5;
        animator.SetInteger(shotTypeHash, selectedShotType);
    }

    public void OnServe()
    {
        if (!isInServeMode || hasServed) return;
        
        // Execute serve
        animator.SetTrigger(serveHash);
        
        // Actual serve is triggered by animation event, but we'll add backup
        StartCoroutine(DelayedServe(0.5f));
        
        hasServed = true;
    }

    private void Move()
    {
        if (movementInput.sqrMagnitude == 0) return;

        // Calculate movement direction
        Vector3 moveDirection = new Vector3(movementInput.x, 0, movementInput.y).normalized;
        
        // Apply sprint multiplier if sprinting
        float currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);
        
        // Move the player
        controller.Move(moveDirection * currentSpeed * Time.deltaTime);
        
        // Rotate towards movement direction
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    private void UpdateServeMode()
    {
        // Limited side-to-side movement in serve mode
        if (Mathf.Abs(movementInput.x) > 0.1f)
        {
            Vector3 sideMovement = new Vector3(movementInput.x, 0, 0).normalized;
            controller.Move(sideMovement * (moveSpeed * 0.5f) * Time.deltaTime);
        }
    }

    private void UpdateShotCharge()
    {
        if (isCharging)
        {
            // Simple charge calculation
            float timeCharging = Time.time - chargeStartTime;
            currentCharge = Mathf.Min(timeCharging * chargeRate, maxPowerCharge);
            
            // Update charge animation
            animator.SetFloat(chargeHash, currentCharge / maxPowerCharge);
            
            // Auto-release if charged too long
            if (currentCharge >= maxPowerCharge)
            {
                ExecuteShot();
                isCharging = false;
            }
        }
    }

    private void ExecuteShot()
    {
        // Only execute shot if we have a target ball in range
        if (targetBall != null && IsInHitRange())
        {
            // Calculate power based on charge time
            float power = Mathf.Lerp(minPower, maxPower, currentCharge / maxPowerCharge);
            
            // Calculate direction based on player's facing and desired target
            Vector3 targetDirection = CalculateShotDirection();
            
            // Apply spin based on shot type
            float spinFactor = GetSpinFactor();
            
            // Trigger hit animation
            animator.SetTrigger(hitTriggerHash);
            
            // Play hit particle effect
            if (hitParticleSystem != null)
            {
                hitParticleSystem.transform.position = racketTransform.position;
                hitParticleSystem.Play();
            }
            
            // Execute actual hit in a delayed manner to match animation
            StartCoroutine(DelayedHit(targetDirection, power, spinFactor));
            
            // Start shot cooldown
            StartCoroutine(ShotCooldown());
        }
        
        // Reset charge animation
        animator.SetFloat(chargeHash, 0);
    }

    private IEnumerator DelayedHit(Vector3 direction, float power, float spinFactor)
    {
        // Delay to match animation timing
        yield return new WaitForSeconds(0.1f);
        
        // Check again if ball is in range (might have moved)
        if (targetBall != null && IsInHitRange())
        {
            // Hit the ball
            targetBall.Hit(direction, power, spinFactor, true);
            
            // Clear target ball
            targetBall = null;
        }
    }

    private IEnumerator ShotCooldown()
    {
        canShoot = false;
        yield return new WaitForSeconds(shotCooldown);
        canShoot = true;
    }

    private bool IsInHitRange()
    {
        if (targetBall == null) return false;
        
        // Check if ball is within hitting range
        float distanceToBall = Vector3.Distance(racketTransform.position, targetBall.transform.position);
        return distanceToBall < 2.0f; // Hitting range
    }

    private Vector3 CalculateShotDirection()
    {
        // Base direction facing opposite court
        Vector3 baseDirection = transform.forward;
        
        // Can be modified based on shot type and court strategy
        if (selectedShotType == 3) // Lob
        {
            // Add vertical component for lobs
            baseDirection += Vector3.up * lobHeight;
        }
        else if (selectedShotType == 4) // Smash
        {
            // Downward trajectory for smashes
            baseDirection += Vector3.down * 0.5f;
        }
        
        return baseDirection.normalized;
    }

    private float GetSpinFactor()
    {
        switch (selectedShotType)
        {
            case 1: return topspinFactor;
            case 2: return backspinFactor;
            case 4: return topspinFactor * smashPowerMultiplier;
            default: return flatFactor;
        }
    }

    private void UpdateAnimations()
    {
        // Update movement animations
        float speed = movementInput.magnitude * (isSprinting ? sprintMultiplier : 1f);
        animator.SetFloat(speedHash, speed, 0.1f, Time.deltaTime);
        
        // Direction can be used for strafing animations
        float direction = Vector3.Dot(transform.right, new Vector3(movementInput.x, 0, movementInput.y).normalized);
        animator.SetFloat(directionHash, direction, 0.1f, Time.deltaTime);
    }

    // Called by trigger collider when ball enters player's reach zone
    public void SetTargetBall(TennisBall ball)
    {
        if (canShoot && !isInServeMode)
        {
            targetBall = ball;
        }
    }

    // Called when ball leaves player's reach zone
    public void ClearTargetBall()
    {
        if (targetBall != null)
        {
            targetBall = null;
        }
    }
    
    // Method for serving
    public void PrepareForServe(TennisBall ball)
    {
        isInServeMode = true;
        hasServed = false;
        targetBall = ball;
    }
    
    public void EnableServeControl(bool enable)
    {
        isInServeMode = enable;
    }
    
    public bool HasServed()
    {
        return hasServed;
    }
    
    public bool IsInServeMode()
    {
        return isInServeMode;
    }
    
    private IEnumerator DelayedServe(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (isInServeMode && hasServed && targetBall != null)
        {
            // Calculate serve direction
            Vector3 serveDirection = transform.forward + new Vector3(0, 0.2f, 0);
            
            // Calculate power (fixed for serves)
            float servePower = Mathf.Lerp(minPower, maxPower, 0.7f);
            
            // Hit the ball for serve
            targetBall.Hit(serveDirection, servePower, flatFactor, true);
        }
    }
    
    // Execute serve (called by animation event ideally)
    public void ExecuteServeFromAnimation()
    {
        if (isInServeMode && targetBall != null)
        {
            StopAllCoroutines(); // Stop delayed serve if running
            
            // Calculate serve direction
            Vector3 serveDirection = transform.forward + new Vector3(0, 0.2f, 0);
            
            // Calculate power (fixed for serves)
            float servePower = Mathf.Lerp(minPower, maxPower, 0.7f);
            
            // Hit the ball for serve
            targetBall.Hit(serveDirection, servePower, flatFactor, true);
        }
    }
    
    public void ResetPlayer()
    {
        // Reset state
        isCharging = false;
        currentCharge = 0f;
        targetBall = null;
        canShoot = true;
        isInServeMode = false;
        hasServed = false;
        
        // Reset animations
        if (animator != null)
        {
            animator.SetFloat(chargeHash, 0f);
            animator.SetFloat(speedHash, 0f);
            animator.SetFloat(directionHash, 0f);
        }
        
        // Reset position (with character controller, we need a different approach)
        StartCoroutine(ResetPosition());
    }
    
    private IEnumerator ResetPosition()
    {
        // Disable character controller temporarily
        controller.enabled = false;
        
        // Wait a frame to ensure controller is disabled
        yield return null;
        
        // Reset position
        transform.position = startPosition;
        
        // Wait for physics to settle
        yield return new WaitForSeconds(positionResetTime);
        
        // Re-enable character controller
        controller.enabled = true;
    }
    
    // Unity event trigger for ball detection - should be attached to a child trigger collider
    private void OnTriggerEnter(Collider other)
    {
        if (!canShoot || isInServeMode) return;
        
        TennisBall ball = other.GetComponent<TennisBall>();
        if (ball != null)
        {
            SetTargetBall(ball);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        TennisBall ball = other.GetComponent<TennisBall>();
        if (ball != null && ball == targetBall)
        {
            ClearTargetBall();
        }
    }
}
