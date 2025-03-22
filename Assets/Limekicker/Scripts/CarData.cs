using UnityEngine;

public class CarData : ScriptableObject
{
    [SerializeField] private int acceleration;
    [SerializeField] private int maxSpeed;
    [SerializeField] private int endurance;
}
