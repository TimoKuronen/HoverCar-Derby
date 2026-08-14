/// <summary>
/// Contract for toggling gameplay HUD and driving controls.
/// </summary>
public interface IGameplayHudView
{
    void SetGameplayHudVisible(bool visible);
    void SetDrivingControlsVisible(bool visible);
}
