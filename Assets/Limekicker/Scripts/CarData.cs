using UnityEngine;

[CreateAssetMenu(menuName = "Limekicker/CarData")]
public class CarData : ScriptableObject
{
    [Range(1, 5), SerializeField] private int acceleration;
    [Range(1, 5), SerializeField] private int maxSpeed;
    [Range(1, 5), SerializeField] private int endurance;

    public float GetAccelerationMultiplier()
    {
        return 1 + (acceleration / 20);
    }

    public float GetMaxSpeedMultiplier()
    {
        return 1 + (maxSpeed / 20);
    }

    public float GetEnduranceMultiplier()
    {
        return 1 + (endurance / 10);
    }

    public void RandomiseCarValues(int pointsToDistribute)
    {
        int minValue = 1, maxValue = 5;
        int remaining = pointsToDistribute;

        acceleration = minValue;
        maxSpeed = minValue;
        endurance = minValue;
        remaining -= 3;

        int[] stats = { acceleration, maxSpeed, endurance };

        System.Random randomInt = new System.Random();

        while (remaining > 0)
        {
            int index = randomInt.Next(0, 3);

            if (stats[index] < maxValue)
            {
                stats[index]++;
                remaining--;
            }
        }

        acceleration = stats[0];
        maxSpeed = stats[1];
        endurance = stats[2];
    }
}
