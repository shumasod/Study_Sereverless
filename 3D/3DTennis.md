# 3Dテニスゲーム 開発計画

## フェーズ1: 基礎システム構築（4週間）

### 週1: プロジェクトセットアップとプレイヤーコントロール
- Unity 2022.3 LTSプロジェクト作成、URPセットアップ
- 基本的なコート作成（ProBuilderを使用）
- プレイヤーキャラクターの基本動作
  - `PlayerController.cs`の実装
  - 入力システムの設定（Input System Package）
  - キャラクターの基本移動機能

### 週2: ボール物理システムと基本アニメーション
- `TennisBall.cs`の実装
  - カスタム物理挙動の設定
  - ボールのスピン効果
  - バウンド処理
- プレイヤーの基本アニメーション
  - Mecanimシステムのセットアップ
  - 基本的な走行アニメーション
  - スイングアニメーション（フォアハンド/バックハンド）

### 週3: カメラシステムとコート境界
- `TennisCamera.cs`の実装
  - プレイヤー追従カメラ
  - ボール追従カメラ
  - カメラモード切り替え
- コート境界の設定
  - アウトライン判定
  - スコアシステムとの連携

### 週4: 基本UI実装とサウンド
- 基本UI要素の実装
  - スコア表示
  - プレイヤー情報
  - メインメニュー
- 基本サウンドの実装
  - ボール打球音
  - フットステップ
  - 環境音

## フェーズ2: ゲームプレイ機能拡張（4週間）

### 週5: AIシステム
- `TennisAI.cs`の実装
  - 基本的なAI移動と判断ロジック
  - 難易度設定
  - プレイスタイルの多様化

### 週6: 試合管理システム
- `MatchManager.cs`の実装
  - スコアシステム
  - ゲーム、セット管理
  - 勝敗判定

### 週7: 拡張アニメーションとビジュアル
- 応用アニメーション
  - 多様なショットタイプ
  - リアクションアニメーション
  - Animation Riggingによる動的な動き
- ビジュアルエフェクト
  - パーティクルエフェクト
  - Shader Graphによるカスタムシェーダー

### 週8: 追加ゲームモード
- トレーニングモード
- クイックマッチ
- トーナメントの基本システム

## フェーズ3: 応用機能と最適化（4週間）

### 週9: ネットワーク機能
- Photon PUN 2のセットアップ
- 基本的なマルチプレイヤー機能
- ネットワーク同期の最適化

### 週10: キャラクターカスタマイズ
- キャラクター選択システム
- 基本的なカスタマイズオプション
- プレイヤープロファイル

### 週11: パフォーマンス最適化
- LODシステムの実装
- オブジェクトプーリング
- モバイル向け最適化

### 週12: ポリッシュとバグ修正
- UI/UXの改善
- 全体的なゲームフィールの調整
- バグ修正とQAテスト

## フェーズ4: 拡張と仕上げ（4週間）

### 週13-14: コンテンツ拡充
- 追加コートタイプ
- 追加キャラクター
- 追加ゲームモード

### 週15: プラットフォーム対応
- コンソール対応
- モバイル対応
- クロスプラットフォームテスト

### 週16: 最終調整とリリース準備
- 最終バランス調整
- アセット最適化
- ビルドとパッケージング

## 技術的な実装詳細

### プレイヤーコントロールシステム
```csharp
// PlayerMovement.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float sprintMultiplier = 1.5f;

    [Header("Components")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Animator animator;

    // Input values
    private Vector2 movementInput;
    private bool isSprinting;

    // Animation parameter hashes
    private int speedHash;
    private int directionHash;

    private void Awake()
    {
        // Get references if not assigned
        if (controller == null) controller = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Cache animation parameter hashes
        speedHash = Animator.StringToHash("Speed");
        directionHash = Animator.StringToHash("Direction");
    }

    private void Update()
    {
        Move();
        UpdateAnimations();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        isSprinting = context.ReadValueAsButton();
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

    private void UpdateAnimations()
    {
        // Set animation parameters
        float speed = movementInput.magnitude * (isSprinting ? sprintMultiplier : 1f);
        animator.SetFloat(speedHash, speed, 0.1f, Time.deltaTime);
        
        // Direction can be used for strafing animations
        float direction = Vector3.Dot(transform.right, new Vector3(movementInput.x, 0, movementInput.y).normalized);
        animator.SetFloat(directionHash, direction, 0.1f, Time.deltaTime);
    }
}
```

### ショット制御システム
```csharp
// PlayerShot.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShot : MonoBehaviour
{
    [Header("Shot Settings")]
    [SerializeField] private float maxPowerCharge = 1.5f;
    [SerializeField] private float chargeRate = 1f;
    [SerializeField] private float minPower = 5f;
    [SerializeField] private float maxPower = 20f;

    [Header("Shot Types")]
    [SerializeField] private float topspinFactor = 0.8f;
    [SerializeField] private float backspinFactor = -0.5f;
    [SerializeField] private float flatFactor = 0.2f;
    [SerializeField] private float lobHeight = 10f;
    [SerializeField] private float smashPowerMultiplier = 1.8f;

    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform racketTransform;

    // State
    private bool isCharging;
    private float currentCharge;
    private TennisBall targetBall;
    private int selectedShotType = 0; // 0=Flat, 1=Topspin, 2=Backspin, 3=Lob, 4=Smash

    // Animation parameter hashes
    private int chargeHash;
    private int shotTypeHash;
    private int hitTriggerHash;

    private void Awake()
    {
        // Get references if not assigned
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Cache animation parameter hashes
        chargeHash = Animator.StringToHash("ChargeAmount");
        shotTypeHash = Animator.StringToHash("ShotType");
        hitTriggerHash = Animator.StringToHash("Hit");
    }

    private void Update()
    {
        if (isCharging)
        {
            currentCharge += chargeRate * Time.deltaTime;
            currentCharge = Mathf.Min(currentCharge, maxPowerCharge);
            
            // Update charge animation
            animator.SetFloat(chargeHash, currentCharge / maxPowerCharge);
        }
    }

    public void OnChargeShotStart(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            BeginChargeShot();
        }
    }

    public void OnChargeShotRelease(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ExecuteShot();
        }
    }

    public void OnChangeShotType(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            // Cycle through shot types
            selectedShotType = (selectedShotType + 1) % 5;
            animator.SetInteger(shotTypeHash, selectedShotType);
        }
    }

    private void BeginChargeShot()
    {
        isCharging = true;
        currentCharge = 0f;
    }

    private void ExecuteShot()
    {
        if (!isCharging) return;
        isCharging = false;
        
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
            
            // Hit the ball
            targetBall.Hit(targetDirection, power, spinFactor);
            
            // Clear target ball
            targetBall = null;
        }
        
        // Reset charge animation
        animator.SetFloat(chargeHash, 0);
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

    // Called by trigger collider when ball enters player's reach zone
    public void SetTargetBall(TennisBall ball)
    {
        targetBall = ball;
    }

    // Called when ball leaves player's reach zone
    public void ClearTargetBall()
    {
        if (targetBall != null)
        {
            targetBall = null;
        }
    }
}
```

### ボール物理システム
```csharp
// TennisBall.cs
using UnityEngine;

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
    [SerializeField] private Rigidbody rb;
    [SerializeField] private TrailRenderer trail;

    // State
    private Vector3 spinVector;
    private CourtType currentCourtType = CourtType.Hard;
    private bool isInPlay = false;

    public enum CourtType { Grass, Clay, Hard, Indoor }
    
    public enum BallState { Idle, InPlay, OutOfBounds }
    private BallState currentState = BallState.Idle;

    private void Awake()
    {
        // Get references if not assigned
        if (rb == null) rb = GetComponent<Rigidbody>();
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
        }
    }

    public void Hit(Vector3 direction, float power, float spinFactor)
    {
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
        isInPlay = true;
        
        // Enable trail
        if (trail != null)
        {
            trail.emitting = true;
        }
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
            // Play bounce sound
            // AudioManager.Instance.PlaySound("BallBounce", transform.position);
            
            // Apply court-specific bounce effects
            if (currentCourtType == CourtType.Clay)
            {
                // Reduce velocity more on clay
                rb.velocity *= 0.9f;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("OutOfBounds") && currentState == BallState.InPlay)
        {
            currentState = BallState.OutOfBounds;
            // Notify game manager
            GameManager.Instance.ProcessOutOfBounds(transform.position);
        }
    }

    public void Reset()
    {
        // Reset ball state for new point
        currentState = BallState.Idle;
        isInPlay = false;
        spinVector = Vector3.zero;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        if (trail != null)
        {
            trail.emitting = false;
            trail.Clear();
        }
    }
}
```

## 続きの実装

次のステップでは:

1. GameManager、MatchManagerの実装
2. AIシステムの実装
3. カメラシステムの高度な制御
4. UIとメニューシステム

を進めていきます。プロンプトに従って、コア機能を実装した後、追加機能や最適化、UIを拡充していくのが良いでしょう。
