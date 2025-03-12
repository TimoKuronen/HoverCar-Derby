using UnityEngine;
using UnityEngine.UI;

public class UISlider : UIInteractable
{
    private Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();
    }
    public void AdjustValue(float direction)
    {
        if((direction > 0 && slider.value == slider.maxValue) || (direction < 0 && slider.value == slider.minValue))
        {
            Debug.Log("can't adjust slider outside range");
            return;
        }
        base.AdjustValue(direction);
        slider.value = Mathf.Clamp01(slider.value + direction * 0.1f);
        Debug.Log($"{name} Adjusted to {slider.value}");
    }
}
