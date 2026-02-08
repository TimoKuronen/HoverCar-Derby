using UnityEngine;
using UnityEngine.UI;

public class GameMenu : MonoBehaviour
{
    [SerializeField] private Button sfxButton;
    [SerializeField] private Button musicButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        sfxButton.onClick.AddListener(ToggleSFX);
        musicButton.onClick.AddListener(ToggleMusic);
        quitButton.onClick.AddListener(QuitGame);
    }

    private void ToggleSFX()
    {
  
    }

    private void ToggleMusic()
    {
        
    }

    private void QuitGame()
    {
        
    }
}
