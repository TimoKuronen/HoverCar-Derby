using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class UIMenu : MonoBehaviour
{
    protected List<IUIInteractable> interactables = new List<IUIInteractable>();
    private int currentIndex = 0;
    public List<GameObject> debuglist = new List<GameObject>();

    public event Action OnMenuOpened;
    public event Action OnMenuClosed;
    public event Action OnItemSelected;
    public event Action OnItemTriggered;

    protected virtual void Init()
    {
        interactables = GetComponentsInChildren<IUIInteractable>(true).ToList();
        foreach (var interactable in interactables)
        {
            if (interactable is MonoBehaviour mono)
            {
                debuglist.Add(mono.gameObject);
            }

            interactable.OnSelect += CheckSelectedIndex;
        }
    }

    /// <summary>
    /// We need to update current index in case we have navigated somewhere and then went elsewhere with mouse
    /// </summary>
    /// <param name="interactable"></param>
    void CheckSelectedIndex(IUIInteractable interactable)
    {
        for (int i = 0; i < interactables.Count; i++)
        {
            if (interactable == interactables[i])
            {
                if (currentIndex != i)
                {
                    currentIndex = i;

                    foreach (var item in interactables)
                        item.Deselect();

                    interactables[currentIndex].Select();
                }

                break;
            }
        }
    }

    protected void OpenMenu()
    {
        OnMenuOpened?.Invoke();
        UpdateSelection();
    }

    protected void CloseMenu()
    {
        OnMenuClosed?.Invoke();
    }

    public void MoveNext()
    {
        currentIndex = (currentIndex + 1) % interactables.Count;
        UpdateSelection();
    }

    public void MovePrevious()
    {
        currentIndex = (currentIndex - 1 + interactables.Count) % interactables.Count;
        UpdateSelection();
    }

    public void SelectCurrent()
    {
        //interactables[currentIndex]?.OnActivate();
    }

    public void TriggerCurrent()
    {
        interactables[currentIndex]?.Activate();
        OnItemTriggered?.Invoke();
    }

    public void AdjustCurrentValue(float direction)
    {
        interactables[currentIndex]?.AdjustValue(direction);
    }

    public void UpdateSelection()
    {
        foreach (var item in interactables)
            item.Deselect();

        interactables[currentIndex].Select();

        OnItemSelected?.Invoke();
    }

    public bool CanIncrementValue()
    {
        return interactables[currentIndex].CanIncrementValue();
    }
}
