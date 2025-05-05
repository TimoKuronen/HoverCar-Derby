using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    [SerializeField] private bool getMouseMovementInput;
    public bool GetMouseMovementInput => getMouseMovementInput;

    public bool IsGameDataLoaded { get; private set; }

    private void Awake()
    {
        Instance = this;
        LayerStorage.SetLayerValues();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Escape key pressed.");
            Debug.Break();
        }
    }
}
