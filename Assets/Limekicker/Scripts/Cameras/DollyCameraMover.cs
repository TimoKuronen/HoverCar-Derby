using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives a Cinemachine dolly camera along a path for intro shots.
/// </summary>
public class DollyCameraMover : MonoBehaviour
{
    public CinemachineVirtualCamera vcam;
    public float speed = 2f;
    private bool moving = false;

    CinemachineTrackedDolly dolly;

    void Awake()
    {
        dolly = vcam.GetCinemachineComponent<CinemachineTrackedDolly>();
    }

    /// <summary>
    /// Toggles dolly camera movement on/off. Resets path position and adjusts camera priority.
    /// </summary>
    public void ToggleMovement()
    {
        moving = !moving;

        if (moving)
        {
            dolly.m_PathPosition = 0f;
            vcam.Priority = 25;
        }
        else
        {
            dolly.m_PathPosition = 0f;
            vcam.Priority = 0;
        }
    }

    void FixedUpdate()
    {
        if (moving)
        {
            dolly.m_PathPosition += speed * Time.deltaTime;
        }
    }
}
