using UnityEngine;

public class RearBumper : CarPart
{
    protected override void OnDestroyed()
    {
        base.OnDestroyed();
        Debug.Log("Rear bumper is gone! Vulnerable to rear hits.");
    }
}
