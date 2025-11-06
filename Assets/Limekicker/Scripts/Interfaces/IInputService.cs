public interface IInputService
{
    float Steering { get; }
    bool IsGasPressed { get; }
    void SetGasPressed(bool value);
}
