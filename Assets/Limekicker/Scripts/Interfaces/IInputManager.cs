public interface IInputManager : IUpdateableService
{
    float GetSteer();
    float GetGas();
    float GetBrake();
    bool GetJump();
}
