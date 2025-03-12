using UnityEngine;

public class UIControllerNavigator : IUIControllerNavigator
{
    private UIMenu activeMenu;

    public void Initialize()
    {
        Services.Get<IInputManager>().OnMoveDown += MoveNext;
        Services.Get<IInputManager>().OnMoveUp += MovePrevious;
        Services.Get<IInputManager>().OnMoveLeft += () => AdjustValue(-1);
        Services.Get<IInputManager>().OnMoveRight += () => AdjustValue(1);
        Services.Get<IInputManager>().OnSubmit += TriggerCurrent;
    }

    public void SetActiveMenu(UIMenu menu)
    {
        activeMenu = menu;
        //Debug.Log(menu.ToString() + " set as current menu item");
    }

    public void MoveNext()
    {
        activeMenu?.MoveNext();
    }

    public void MovePrevious()
    {
        activeMenu?.MovePrevious();
    }

    public void SelectCurrent()
    {
        activeMenu?.SelectCurrent();
    }
    public void TriggerCurrent()
    {
        activeMenu?.TriggerCurrent();
    }

    public void AdjustValue(float direction)
    {
        if (activeMenu != null)
        {
            if (activeMenu.CanIncrementValue())
                activeMenu.AdjustCurrentValue(direction);

            else if (direction > 0)
                MoveNext();
            else MovePrevious();
        }
    }
}
