using UnityEngine;

public class CoroutineMonoBehavior : MonoBehaviour
{
    public static CoroutineMonoBehavior Instance;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this.gameObject);
            return;
        }
    }
}