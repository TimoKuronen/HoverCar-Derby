public interface IUIControllerNavigator : IService
{
    void MoveNext();
    void MovePrevious();
    void SelectCurrent();
    void AdjustValue(float direction);
    void SetActiveMenu(UIMenu menu);
    void TriggerCurrent();
}
