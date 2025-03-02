// CameraManager.cs
using System.Collections;
using UnityEngine;
using Cinemachine;

public class CameraManager : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private CinemachineVirtualCamera playerFollowCamera;
    [SerializeField] private CinemachineVirtualCamera ballFollowCamera;
    [SerializeField] private CinemachineVirtualCamera broadcastCamera;
    [SerializeField] private CinemachineVirtualCamera serveCam;
    [SerializeField] private CinemachineVirtualCamera cinematicCam;
    [SerializeField] private CinemachineVirtualCamera menuCam;
    
    [Header("Camera Settings")]
    [SerializeField] private float transitionSpeed = 2f;
    [SerializeField] private float menuCameraRotationSpeed = 0.2f;
    
    // カメラモード
    public enum CameraMode
    {
        FollowPlayer,
        FollowBall,
        BroadcastView,
        ServeCam,
        CinematicView,
        MenuView
    }
    
    // 現在のカメラモード
    private CameraMode currentMode = CameraMode.MenuView;
    
    // ターゲット参照
    private Transform playerTarget;
    private Transform aiTarget;
    private Transform ballTarget;
    private Transform courtCenter;
    
    // 切り替えアニメーションフラグ
    private bool isTransitioning = false;
    
    // カメラ優先度の基本値
    private const int BasePriority = 10;
    private const int ActivePriority = 20;
    
    private void Awake()
    {
        // カメラの優先度を初期化
        InitializeCameraPriorities();
    }
    
    private void Start()
    {
        // 開始時はメニュービューを使用
        SwitchCameraMode(CameraMode.MenuView);
        
        // メニューカメラの回転アニメーション開始
        StartCoroutine(AnimateMenuCamera());
    }
    
    // カメラの優先度を初期化
    private void InitializeCameraPriorities()
    {
        playerFollowCamera.Priority = BasePriority;
        ballFollowCamera.Priority = BasePriority;
        broadcastCamera.Priority = BasePriority;
        serveCam.Priority = BasePriority;
        cinematicCam.Priority = BasePriority;
        menuCam.Priority = BasePriority;
    }
    
    // カメラのターゲットを設定
    public void SetTargets(Transform player, Transform ai, Transform court)
    {
        playerTarget = player;
        aiTarget = ai;
        courtCenter = court;
        
        // カメラのターゲット設定
        if (playerFollowCamera != null && playerFollowCamera.Follow != playerTarget)
        {
            playerFollowCamera.Follow = playerTarget;
        }
        
        if (broadcastCamera != null && broadcastCamera.LookAt != courtCenter)
        {
            broadcastCamera.LookAt = courtCenter;
        }
        
        // その他のカメラのターゲット設定
    }
    
    // ボールのターゲットを設定
    public void SetBallTarget(Transform ball)
    {
        ballTarget = ball;
        
        if (ballFollowCamera != null && ballFollowCamera.Follow != ballTarget)
        {
            ballFollowCamera.Follow = ballTarget;
        }
    }
    
    // カメラモードの切り替え
    public void SwitchCameraMode(CameraMode newMode)
    {
        if (currentMode == newMode || isTransitioning) return;
        
        // 古いモードのカメラ優先度を下げる
        LowerPriority(currentMode);
        
        // 新しいモードのカメラ優先度を上げる
        RaisePriority(newMode);
        
        // モードを更新
        currentMode = newMode;
        
        // カメラが切り替わるまでの短い遅延
        StartCoroutine(TransitionDelay());
    }
    
    // 一定時間カメラ切り替えをブロック
    private IEnumerator TransitionDelay()
    {
        isTransitioning = true;
        yield return new WaitForSeconds(0.5f);
        isTransitioning = false;
    }
    
    // 指定モードのカメラの優先度を下げる
    private void LowerPriority(CameraMode mode)
    {
        switch (mode)
        {
            case CameraMode.FollowPlayer:
                playerFollowCamera.Priority = BasePriority;
                break;
            case CameraMode.FollowBall:
                ballFollowCamera.Priority = BasePriority;
                break;
            case CameraMode.BroadcastView:
                broadcastCamera.Priority = BasePriority;
                break;
            case CameraMode.ServeCam:
                serveCam.Priority = BasePriority;
                break;
            case CameraMode.CinematicView:
                cinematicCam.Priority = BasePriority;
                break;
            case CameraMode.MenuView:
                menuCam.Priority = BasePriority;
                break;
        }
    }
    
    // 指定モードのカメラの優先度を上げる
    private void RaisePriority(CameraMode mode)
    {
        switch (mode)
        {
            case CameraMode.FollowPlayer:
                playerFollowCamera.Priority = ActivePriority;
                break;
            case CameraMode.FollowBall:
                if (ballTarget != null)
                {
                    ballFollowCamera.Follow = ballTarget;
                }
                ballFollowCamera.Priority = ActivePriority;
                break;
            case CameraMode.BroadcastView:
                broadcastCamera.Priority = ActivePriority;
                break;
            case CameraMode.ServeCam:
                serveCam.Priority = ActivePriority;
                break;
            case CameraMode.CinematicView:
                cinematicCam.Priority = ActivePriority;
                break;
            case CameraMode.MenuView:
                menuCam.Priority = ActivePriority;
                break;
        }
    }
    
    // メニューカメラのアニメーション（ゆっくりと回転）
    private IEnumerator AnimateMenuCamera()
    {
        if (menuCam == null || menuCam.GetComponentInChildren<CinemachineOrbitalTransposer>() == null)
        {
            yield break;
        }
        
        var orbitalTransposer = menuCam.GetComponentInChildren<CinemachineOrbitalTransposer>();
        
        while (true)
        {
            // メニューカメラの場合のみ回転
            if (currentMode == CameraMode.MenuView)
            {
                // XAxisの角度を少しずつ更新
                float currentAngle = orbitalTransposer.m_XAxis.Value;
                orbitalTransposer.m_XAxis.Value = currentAngle + menuCameraRotationSpeed;
            }
            
            yield return null;
        }
    }
    
    // カットシーンの再生
    public IEnumerator PlayCutscene(CutsceneType type)
    {
        // 現在のモードを記憶
        CameraMode previousMode = currentMode;
        
        // シネマティックカメラに切り替え
        SwitchCameraMode(CameraMode.CinematicView);
        
        // カットシーンの種類に応じたカメラワーク
        switch (type)
        {
            case CutsceneType.MatchIntro:
                yield return StartCoroutine(PlayMatchIntroCutscene());
                break;
            case CutsceneType.PointWon:
                yield return StartCoroutine(PlayPointWonCutscene());
                break;
            case CutsceneType.MatchWon:
                yield return StartCoroutine(PlayMatchWonCutscene());
                break;
        }
        
        // 元のカメラモードに戻る
        SwitchCameraMode(previousMode);
    }
    
    // 試合開始カットシーン
    private IEnumerator PlayMatchIntroCutscene()
    {
        // コートのパン&ズーム
        
        // カメラの初期位置から移動先への遷移を計算
        yield return new WaitForSeconds(3f);
    }
    
    // ポイント獲得カットシーン
    private IEnumerator PlayPointWonCutscene()
    {
        // プレイヤーにフォーカス
        
        // 短いアクションシーケンス
        yield return new WaitForSeconds(1.5f);
    }
    
    // 試合勝利カットシーン
    private IEnumerator PlayMatchWonCutscene()
    {
        // 勝者にフォーカス
        
        // 勝利セレブレーション
        yield return new WaitForSeconds(5f);
    }
    
    // カットシーンの種類
    public enum CutsceneType
    {
        MatchIntro,
        PointWon,
        MatchWon
    }
    
    // サービスカメラに切り替え
    public void SetupServeCam(Transform server)
    {
        // サービングプレイヤーに対するカメラの微調整
        if (serveCam != null)
        {
            serveCam.Follow = server;
            serveCam.LookAt = server;
            
            // カメラの位置調整（必要に応じて）
            CinemachineTransposer transposer = serveCam.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer != null)
            {
                // サーバーがプレイヤーかAIかによって位置を調整
                if (server == playerTarget)
                {
                    transposer.m_FollowOffset = new Vector3(0, 2, -3);
                }
                else
                {
                    transposer.m_FollowOffset = new Vector3(0, 2, 3);
                }
            }
        }
    }
    
    // リプレイカメラの設定
    public void SetupReplayCameras(Vector3 ballHitPos, Vector3 ballLandPos)
    {
        // リプレイ用のカメラ設定
        // ヒット位置とボール着地位置に基づいてカメラを設定
    }
}
