/// <summary>
/// Local pause overlay. Match time and physics keep running; gameplay systems
/// gate input while this state is active (e.g. HoverCarMover only drives in PlayState).
/// </summary>
public class PauseState : IGameState
{
    public void Enter() { }

    public void Exit() { }

    public void Update() { }
}
