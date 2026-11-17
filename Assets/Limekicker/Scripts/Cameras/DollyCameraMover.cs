using Cinemachine;
using UnityEngine;

/// <summary>
/// Owns a Cinemachine dolly camera: shows it at the start of its track, optionally runs it along the
/// track, and hides it again. Callers choose the priority tier when showing it.
/// </summary>
public class DollyCameraMover : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera vcam;
    [SerializeField] private float speed = 2f;

    private CinemachineTrackedDolly dolly;
    private bool moving;

    private void Awake()
    {
        if (vcam != null)
            dolly = vcam.GetCinemachineComponent<CinemachineTrackedDolly>();

        if (dolly == null)
        {
            Debug.LogError($"{nameof(DollyCameraMover)} on '{name}' needs a virtual camera with a Tracked Dolly body.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (moving)
            dolly.m_PathPosition += speed * Time.deltaTime;
    }

    /// <summary>
    /// Takes the view at the given priority, parked at the start of the track.
    /// </summary>
    public void Show(int priority)
    {
        if (dolly == null)
            return;

        moving = false;
        dolly.m_PathPosition = 0f;
        vcam.Priority = priority;
    }

    /// <summary>
    /// Starts moving along the track from the current position.
    /// </summary>
    public void Play()
    {
        moving = dolly != null;
    }

    /// <summary>
    /// Stops, rewinds to the start of the track and gives up the view.
    /// </summary>
    public void Hide()
    {
        if (dolly == null)
            return;

        moving = false;
        dolly.m_PathPosition = 0f;
        vcam.Priority = CameraPriorities.Inactive;
    }

    public void SetLookAt(Transform target)
    {
        if (vcam != null)
            vcam.LookAt = target;
    }
}
