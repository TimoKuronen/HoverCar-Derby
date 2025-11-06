using VContainer.Unity;

public interface IInputService : ITickable
{
    float Steering { get; }
    bool IsGasPressed { get; }
    void SetGasPressed(bool value);
}
