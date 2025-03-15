using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NitroBoost : MonoBehaviour
{
    [SerializeField] private float maxNitroAmount;
    [SerializeField] private float currentNitroAmount;
    [SerializeField] private float nitroBurnRate;
    [SerializeField] private float minimumNitroRequiredToBurn;
    [SerializeField] private float regenerationRate;
    [SerializeField] private float cooldownDuration;

    [SerializeField] private float accelerationMultiplierValue;
    [SerializeField] private float maxSpeedMultiplier;

    private HoverCarControl hoverCarControl;
    private bool nitroBoostActivated;
    private float cooldownTimer;

    private void Start()
    {
        hoverCarControl = GetComponent<HoverCarControl>();
        cooldownTimer = cooldownDuration;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (currentNitroAmount > minimumNitroRequiredToBurn && cooldownTimer > cooldownDuration)
                ToggleNitro();
        }
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            ToggleNitro();
        }
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

    private void ToggleNitro()
    {
        nitroBoostActivated = !nitroBoostActivated;

        hoverCarControl.ToggleNitroBoost(nitroBoostActivated, accelerationMultiplierValue, maxSpeedMultiplier);
    }
}
