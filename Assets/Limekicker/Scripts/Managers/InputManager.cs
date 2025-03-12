using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class InputManager : IInputManager
{
    public InputMethod CurrentInputMethod { get; private set; }
    public Vector2 MousePosition { get; private set; }
    public Action<Vector3, GameObject> OnLeftMouseButton { get; private set; }
    public Action OnCancel { get; set; }
    public Action OnMoveDown { get; set; }
    public Action OnMoveUp { get; set; }
    public Action OnMoveLeft { get; set; }
    public Action OnMoveRight { get; set; }
    public Action OnSubmit { get; set; }

    private bool canNavigateUI;
    private float lastInputTime;
    private float inputCooldown = 0.2f;

    private InputActionAsset inputActions;
    private InputActionMap uiActionMap;
    private InputActionMap playerActionMap;

    private InputAction moveAction;
    private InputAction lookAction;

    public void Initialize()
    {

    }

    public void AddInputReference(InputActionAsset inputAsset)
    {
        inputActions = inputAsset;
        uiActionMap = inputActions.FindActionMap("UI");
        playerActionMap = inputActions.FindActionMap("Player");

        uiActionMap.FindAction("Navigate").performed += ctx => HandleNavigation(ctx);
        uiActionMap.FindAction("Submit").performed += ctx => OnSubmit?.Invoke();
        uiActionMap.FindAction("Cancel").performed += ctx => OnCancel?.Invoke();

        moveAction = playerActionMap.FindAction("Move");
        lookAction = playerActionMap.FindAction("Look");

        uiActionMap.Enable();
        playerActionMap.Enable();
    }

    public Vector2 GetMoveInput()
    {
        return moveAction.ReadValue<Vector2>();
    }

    void IUpdateableService.Update()
    {
        if (inputActions == null)
            return;
    }

    void HandleNavigation(InputAction.CallbackContext context)
    {
        Vector2 navigationInput = context.ReadValue<Vector2>();

        if (!context.performed)
        {
            //Debug.Log("prevent navigation because we are at phase: " + context.phase);
            return;
        }
        //else Debug.Log(context.phase);

        if (Time.unscaledTime - lastInputTime < inputCooldown)
            return;

        if (navigationInput.magnitude == 0)
            return;

        if (navigationInput.y > 0)
            OnMoveUp?.Invoke();
        else if (navigationInput.y < 0)
            OnMoveDown?.Invoke();

        if (navigationInput.x > 0)
            OnMoveRight?.Invoke();
        else if (navigationInput.x < 0)
            OnMoveLeft?.Invoke();

        lastInputTime = Time.unscaledTime;
    }
}