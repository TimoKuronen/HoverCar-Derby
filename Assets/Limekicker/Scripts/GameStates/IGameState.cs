/// <summary>
/// Contract for enter, update, and exit lifecycle of a gameplay state.
/// </summary>
public interface IGameState
{
    void Enter();
    void Update();
    void Exit();
}
