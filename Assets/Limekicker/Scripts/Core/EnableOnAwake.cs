using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Toggles configured objects active or inactive when the scene loads.
/// </summary>
public class EnableOnAwake : MonoBehaviour
{
    [SerializeField] private GameObject[] objectsToEnable;
    [SerializeField] private GameObject[] objectsToDisable;

    private void Awake()
    {
        foreach (GameObject obj in objectsToEnable)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }

        foreach (GameObject obj in objectsToDisable)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }
}
