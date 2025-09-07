using UnityEngine;

public class Hull : CarPart
{
    protected override void OnDestroyed()
    {
        base.OnDestroyed();
        Debug.Log("Hull destroyed");
    }
}
