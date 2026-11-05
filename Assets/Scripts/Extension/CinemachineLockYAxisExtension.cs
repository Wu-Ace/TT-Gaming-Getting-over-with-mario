using Cinemachine;
using UnityEngine;

namespace Extension
{
    /// <summary>
    /// An add-on module for Cinemachine Virtual Camera that locks the camera's Y co-ordinate
    /// </summary>
    [ExecuteInEditMode]
    [SaveDuringPlay]
    [AddComponentMenu("")] // Hide in menu
    public class CinemachineLockYAxisExtension : CinemachineExtension
    {
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase virtualCamera,
            CinemachineCore.Stage stage,
            ref CameraState state,
            float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Body)
            {
                var newRawPosition = state.RawPosition;
                state.RawPosition = new Vector3(newRawPosition.x, transform.position.y, newRawPosition.z);
            }
        }
    }
}