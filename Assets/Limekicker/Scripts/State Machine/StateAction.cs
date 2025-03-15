using UnityEngine;
public abstract class StateAction : ScriptableObject
{
    public abstract void Act(StateController controller);
}
