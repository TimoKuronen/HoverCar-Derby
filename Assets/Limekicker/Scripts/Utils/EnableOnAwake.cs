using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableOnAwake : MonoBehaviour
{
    [SerializeField] private GameObject[] targetsToEnable;
    [SerializeField] private GameObject[] targetsToDisable;

    private void Awake()
    {
        foreach (GameObject target in targetsToEnable)
        {
            target.SetActive(true);
        }

        foreach (GameObject target in targetsToDisable)
        {
            target.SetActive(false);
        }
    }
}
