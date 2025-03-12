using UnityEngine;

public class FrontBumper : CarPart
{
    protected override void OnDestroyed()
    {
        base.OnDestroyed();
        Debug.Log("Front bumper is wrecked! Steering may be affected.");
    }
}
