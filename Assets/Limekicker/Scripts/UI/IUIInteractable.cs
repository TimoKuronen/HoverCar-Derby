using System;

public interface IUIInteractable
{
    void Select();
    void Deselect();
    void Activate();
    void AdjustValue(float direction);
    bool CanIncrementValue();

    Action<IUIInteractable> OnSelect { get; set; }
    Action OnDeselect { get; set; }
    Action OnActivate { get; set; }
    Action<float> OnAdjustValue { get; set; }
}
