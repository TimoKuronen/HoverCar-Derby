public class InputManager : IInputManager
{
    private readonly InputSystem_Actions actions;

    private float steerInput;
    private float gasInput;
    private float brakeInput;
    private bool jumpPressed;

    public InputManager()
    {
        UnityEngine.Debug.Log("InputManager Constructor");
        actions = new InputSystem_Actions();
        actions.Enable();

        actions.Player.Steer.performed += ctx => steerInput = ctx.ReadValue<float>();
        actions.Player.Steer.performed += ctx => steerInput = 0;

        actions.Player.Gas.performed += ctx => gasInput = 1;
        actions.Player.Gas.canceled += ctx => gasInput = 0;

        actions.Player.Jump.performed += ctx => jumpPressed = true;
        actions.Player.Jump.canceled += ctx => jumpPressed = false;
    }

    public float GetSteer() => steerInput;
    public float GetGas() => gasInput;
    public float GetBrake() => brakeInput;
    public bool GetJump() => jumpPressed;

    public void Update()
    {

    }
}