using UnityEngine;

/// <summary>
/// Manages nitro fuel, cooldown, and temporary speed boost activation.
/// </summary>
public class NitroBoost : MonoBehaviour
{
    [SerializeField] private float maxNitroAmount;
    [SerializeField] private float nitroBurnRate;
    [SerializeField] private float minimumNitroRequiredToBurn;
    [SerializeField] private float regenerationRate;
    [SerializeField] private float cooldownDuration;

    [SerializeField] private float accelerationMultiplierValue;
    [SerializeField] private float maxSpeedMultiplier;

    private HoverCarMover hoverCarControl;
    private bool nitroBoostActivated;
    private float cooldownTimer;
    private float currentNitroAmount;

    private void Start()
    {
        hoverCarControl = GetComponent<HoverCarMover>();
        cooldownTimer = cooldownDuration;
        currentNitroAmount = maxNitroAmount;
    }

    private void FixedUpdate()
    {
        if (nitroBoostActivated)
        {
            cooldownTimer = 0;

            if (currentNitroAmount < 0)
                ToggleNitro();
            else currentNitroAmount -= nitroBurnRate * Time.deltaTime;
        }
        else if (currentNitroAmount < maxNitroAmount)
        {
            cooldownTimer += Time.deltaTime;

            currentNitroAmount += Time.deltaTime * regenerationRate;
            if (currentNitroAmount > maxNitroAmount)
                currentNitroAmount = maxNitroAmount;
        }
    }

    public void ToggleNitro()
    {
        nitroBoostActivated = !nitroBoostActivated;

        hoverCarControl.ToggleNitroBoost(nitroBoostActivated, accelerationMultiplierValue, maxSpeedMultiplier);
    }

    public bool CanUse()
    {
        return currentNitroAmount > minimumNitroRequiredToBurn && cooldownTimer > cooldownDuration;
    }
}
