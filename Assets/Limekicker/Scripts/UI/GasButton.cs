using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;

public class GasButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private IInputService inputService;

    [Inject]
    public void Construct(IInputService inputService)
    {
        Debug.Log("[GasButton] Constructed");
        this.inputService = inputService;
    }

    public void OnPointerDown(PointerEventData eventData) => inputService.SetGasPressed(true);
    public void OnPointerUp(PointerEventData eventData) => inputService.SetGasPressed(false);
}