// PlayerInputController.cs
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerInputController : MonoBehaviour
{
    // 参照
    private PlayerController playerController;
    private PlayerInput playerInput;
    private GameManager gameManager;
    
    // 入力アクション参照
    private InputAction moveAction;
    private InputAction chargeAction;
    private InputAction shotTypeAction;
    private InputAction serveAction;
    private InputAction pauseAction;

    private void Awake()
    {
        // コンポーネント参照の取得
        playerController = GetComponent<PlayerController>();
        playerInput = GetComponent<PlayerInput>();
        gameManager = FindObjectOfType<GameManager>();
        
        // 入力アクションの取得
        moveAction = playerInput.actions["Move"];
        chargeAction = playerInput.actions["ChargeShot"];
        shotTypeAction = playerInput.actions["ShotType"];
        serveAction = playerInput.actions["Serve"];
        pauseAction = playerInput.actions["Pause"];
    }

    private void OnEnable()
    {
        // イベントリスナーの登録
        if (moveAction != null)
            moveAction.performed += OnMove;
        
        if (chargeAction != null)
        {
            chargeAction.started += OnChargeShotStart;
            chargeAction.canceled += OnChargeShotRelease;
        }
        
        if (shotTypeAction != null)
            shotTypeAction.performed += OnShotTypeChange;
        
        if (serveAction != null)
            serveAction.performed += OnServe;
        
        if (pauseAction != null)
            pauseAction.performed += OnPause;
    }

    private void OnDisable()
    {
        // イベントリスナーの解除
        if (moveAction != null)
            moveAction.performed -= OnMove;
        
        if (chargeAction != null)
        {
            chargeAction.started -= OnChargeShotStart;
            chargeAction.canceled -= OnChargeShotRelease;
        }
        
        if (shotTypeAction != null)
            shotTypeAction.performed -= OnShotTypeChange;
        
        if (serveAction != null)
            serveAction.performed -= OnServe;
        
        if (pauseAction != null)
            pauseAction.performed -= OnPause;
    }

    // 移動入力処理
    private void OnMove(InputAction.CallbackContext context)
    {
        if (playerController != null)
        {
            Vector2 input = context.ReadValue<Vector2>();
            playerController.OnMove(input);
        }
    }

    // ショット充電開始
    private void OnChargeShotStart(InputAction.CallbackContext context)
    {
        if (playerController != null)
        {
            playerController.OnChargeShotStart();
        }
    }

    // ショット実行
    private void OnChargeShotRelease(InputAction.CallbackContext context)
    {
        if (playerController != null)
        {
            playerController.OnChargeShotRelease();
        }
    }

    // ショットタイプ変更
    private void OnShotTypeChange(InputAction.CallbackContext context)
    {
        if (playerController != null)
        {
            playerController.OnChangeShotType();
        }
    }

    // サーブ実行
    private void OnServe(InputAction.CallbackContext context)
    {
        if (playerController != null && playerController.IsInServeMode())
        {
            playerController.OnServe();
        }
    }

    // ポーズ
    private void OnPause(InputAction.CallbackContext context)
    {
        if (gameManager != null)
        {
            gameManager.TogglePause();
        }
    }
}

// TennisInputActions.inputactions (Input System Asset)
// ここでは疑似コードとしてJSONで表現
/*
{
    "name": "TennisInputActions",
    "maps": [
        {
            "name": "Player",
            "id": "3e16727a-3e26-4ff3-8efd-c5a438980afa",
            "actions": [
                {
                    "name": "Move",
                    "type": "Value",
                    "id": "aae094c0-c08c-4c5a-bee0-fa24cc1826c8",
                    "expectedControlType": "Vector2",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": true
                },
                {
                    "name": "ChargeShot",
                    "type": "Button",
                    "id": "e5b9d3c0-2d75-42d7-a3a4-4ff0d7bfce75",
                    "expectedControlType": "Button",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                },
                {
                    "name": "ShotType",
                    "type": "Button",
                    "id": "2e8b46a0-8d7b-4a11-a404-97d7d5cd5c2b",
                    "expectedControlType": "Button",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                },
                {
                    "name": "Serve",
                    "type": "Button",
                    "id": "f5ec7928-3a70-4a2b-9e1a-45cef7c70a3c",
                    "expectedControlType": "Button",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                },
                {
                    "name": "Pause",
                    "type": "Button",
                    "id": "39d1f490-7f75-4d08-86f9-28723e1c294d",
                    "expectedControlType": "Button",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                }
            ],
            "bindings": [
                {
                    "name": "WASD",
                    "id": "5a758b98-8f1c-4f8b-ab41-df3219af2b89",
                    "path": "2DVector",
                    "interactions": "",
                    "processors": "",
                    "groups": "",
                    "action": "Move",
                    "isComposite": true,
                    "isPartOfComposite": false
                },
                {
                    "name": "up",
                    "id": "1a8f5b65-6f2f-4a2a-9c4e-dc7a3ef10247",
                    "path": "<Keyboard>/w",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "Move",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "down",
                    "id": "8fab3cc0-c1ba-4a27-9c2c-0e81c0a5b54a",
                    "path": "<Keyboard>/s",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "Move",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "left",
                    "id": "2cf8da9a-ab0e-45a5-9eb8-b6a2fca4b5f9",
                    "path": "<Keyboard>/a",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "Move",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "right",
                    "id": "aa6af251-89d7-4ef5-b2f3-20e6f4a4b49e",
                    "path": "<Keyboard>/d",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "Move",
                    "isComposite": false,
                    "isPartOfComposite": true
                },
                {
                    "name": "",
                    "id": "dac3b97d-1f2d-4c40-9a7a-7d4c75c5462a",
                    "path": "<Gamepad>/leftStick",
                    "interactions": "",
                    "processors": "",
                    "groups": "Gamepad",
                    "action": "Move",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "0b7cb0e7-08d1-4152-b3eb-2170a3622df6",
                    "path": "<Mouse>/leftButton",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "ChargeShot",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "bf7d7c87-4a15-4e0f-9248-4c7edfa57a67",
                    "path": "<Gamepad>/buttonSouth",
                    "interactions": "",
                    "processors": "",
                    "groups": "Gamepad",
                    "action": "ChargeShot",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "36c90ee1-5f44-4d5c-bad5-d8197fbd1f34",
                    "path": "<Mouse>/rightButton",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "ShotType",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "c7c3a4db-2e38-4e34-bc0c-2adaf3c76c44",
                    "path": "<Gamepad>/buttonEast",
                    "interactions": "",
                    "processors": "",
                    "groups": "Gamepad",
                    "action": "ShotType",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "48a0f50e-9326-46f8-8a26-74aad31631ca",
                    "path": "<Keyboard>/space",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "Serve",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "a3892c5a-a6c9-41c9-93e7-0bc32e8d1b23",
                    "path": "<Gamepad>/buttonNorth",
                    "interactions": "",
                    "processors": "",
                    "groups": "Gamepad",
                    "action": "Serve",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "b7fcb5ee-16d4-4e81-a80a-6f1d77a3b97e",
                    "path": "<Keyboard>/escape",
                    "interactions": "",
                    "processors": "",
                    "groups": "Keyboard&Mouse",
                    "action": "Pause",
                    "isComposite": false,
                    "isPartOfComposite": false
                },
                {
                    "name": "",
                    "id": "fa77cc57-0c6e-43ff-93bc-14a0b9ebf5a5",
                    "path": "<Gamepad>/start",
                    "interactions": "",
                    "processors": "",
                    "groups": "Gamepad",
                    "action": "Pause",
                    "isComposite": false,
                    "isPartOfComposite": false
                }
            ]
        }
    ],
    "controlSchemes": [
        {
            "name": "Keyboard&Mouse",
            "bindingGroup": "Keyboard&Mouse",
            "devices": [
                {
                    "devicePath": "<Keyboard>",
                    "isOptional": false,
                    "isOR": false
                },
                {
                    "devicePath": "<Mouse>",
                    "isOptional": false,
                    "isOR": false
                }
            ]
        },
        {
            "name": "Gamepad",
            "bindingGroup": "Gamepad",
            "devices": [
                {
                    "devicePath": "<Gamepad>",
                    "isOptional": false,
                    "isOR": false
                }
            ]
        }
    ]
}
*/
