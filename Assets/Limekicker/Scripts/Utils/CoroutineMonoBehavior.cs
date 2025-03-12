using UnityEngine;

public class CoroutineMonoBehavior : MonoBehaviour
{
    public static CoroutineMonoBehavior Instance;
    public CoroutineMonoBehavior()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}