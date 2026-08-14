using UnityEngine;

public class GameplayHudView : MonoBehaviour, IGameplayHudView
{
    [SerializeField] private GameObject timePanelRoot;
    [SerializeField] private GameObject pauseMenuButton;
    [SerializeField] private GameObject steeringWheelRoot;
    [SerializeField] private GameObject gasButtonRoot;

    public void SetGameplayHudVisible(bool visible)
    {
        if (timePanelRoot != null)
            timePanelRoot.SetActive(visible);

        if (pauseMenuButton != null)
            pauseMenuButton.SetActive(visible);
    }

    public void SetDrivingControlsVisible(bool visible)
    {
        if (steeringWheelRoot != null)
            steeringWheelRoot.SetActive(visible);

        if (gasButtonRoot != null)
            gasButtonRoot.SetActive(visible);
    }
}
