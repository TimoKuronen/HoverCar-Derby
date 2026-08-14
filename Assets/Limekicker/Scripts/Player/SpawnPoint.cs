using UnityEngine;

/// <summary>
/// Scene marker for player spawn locations.
/// </summary>
public class SpawnPoint : MonoBehaviour
{
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 1f);
    }
#endif
}