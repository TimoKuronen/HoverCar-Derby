using System;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class UIInteractable : MonoBehaviour, IUIInteractable, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
{
    public Action<IUIInteractable> OnSelect { get; set; }
    public Action OnDeselect { get; set; }
    public Action OnActivate { get; set; }
    public Action<float> OnAdjustValue { get; set; }

    public void Select()
    {
        //Debug.Log(gameObject.name + " was selected");
        OnSelect?.Invoke(this);
    }
    public void Deselect()
    {
        OnDeselect?.Invoke();
    }

    public void Activate()
    {
        //Debug.Log(gameObject.name + " was triggered");
        OnActivate?.Invoke();
    }

    public void AdjustValue(float direction) 
    { 
        OnAdjustValue?.Invoke(direction);
    }

    public bool CanIncrementValue()
    {
        return false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Select();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Activate();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Deselect();
    }
}
