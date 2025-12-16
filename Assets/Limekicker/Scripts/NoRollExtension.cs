using Cinemachine;
using UnityEngine;

[ExecuteAlways]
public class NoRollExtension : CinemachineExtension
{
    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage == CinemachineCore.Stage.Body)
        {
            var rot = state.RawOrientation.eulerAngles;
            state.RawOrientation = Quaternion.Euler(0f, rot.y, 0f);
        }
    }
}