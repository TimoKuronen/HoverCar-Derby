using UnityEngine;
using VContainer;

public class SteeringWheel : MonoBehaviour
{
    [SerializeField] private RectTransform steeringWheelUI;

    [SerializeField] private float wheelTurnRange = 60f;
    [SerializeField] private float wheelLerpSpeed = 10f;

    private IInputService inputService;
    private float currentWheelAngle = 0f;

    [Inject]
    public void Construct(IInputService inputService)
    {
        this.inputService = inputService;
    }

    void Update()
    {
        UpdateSteeringWheelVisual(inputService.Steering);
    }

    /// <summary>
    /// Updates steering wheel UI rotation to match input. Uses smooth interpolation for visual feedback.
    /// </summary>
    private void UpdateSteeringWheelVisual(float steerInput)
    {
        steerInput = Mathf.Clamp(steerInput, -1f, 1f);

        float targetAngle = -steerInput * wheelTurnRange;

        currentWheelAngle = Mathf.Lerp(currentWheelAngle, targetAngle, Time.deltaTime * wheelLerpSpeed);

        steeringWheelUI.localRotation = Quaternion.Euler(0f, 0f, currentWheelAngle);
    }
}
