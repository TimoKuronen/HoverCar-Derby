using UnityEngine;

public class SidePanel : CarPart
{
    protected override void OnDestroyed()
    {
        base.OnDestroyed();
        Debug.Log("Side panel destroyed! Exposed to more damage.");
    }
}
