using VContainer.Unity;

/// <summary>
/// Contract for steering and gas input consumed by hover car movement.
/// </summary>
public interface IInputService : ITickable
{
    float Steering { get; }
    bool IsGasPressed { get; }
    void SetGasPressed(bool value);
}
