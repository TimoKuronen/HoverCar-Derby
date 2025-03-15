using UnityEngine;

[CreateAssetMenu(menuName = "Limekicker/State")]
public class State : ScriptableObject
{
    public StateAction[] actions;
    public Transition[] transitions;
    public State parentState;

    public void UpdateState(StateController controller)
    {
        if (parentState != null)
        {
            // Call parent state logic first
            parentState.UpdateState(controller);
        }

        // Then call the sub-state actions/transitions
        DoActions(controller);
        CheckTransitions(controller);
    }

    private void DoActions(StateController controller)
    {
        for (int i = 0; i < actions.Length; i++)
        {
            actions[i].Act(controller);
        }
    }

    private void CheckTransitions(StateController controller)
    {
        for (int i = 0; i < transitions.Length; i++)
        {
            bool decisionSucceeded = transitions[i].decision.Decide(controller);

            if (decisionSucceeded)
            {
                controller.TransitionToState(transitions[i].trueState);
            }
            else
            {
                controller.TransitionToState(transitions[i].falseState);
            }
        }
    }
}