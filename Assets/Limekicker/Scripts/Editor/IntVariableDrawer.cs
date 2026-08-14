using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UIToolkit drawer for IntVariable ScriptableObject references.
/// </summary>
public class IntVariableDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var container = new VisualElement();

        var objectField = new ObjectField(property.displayName)
        {
            objectType = typeof(IntVariable),
        };

        objectField.BindProperty(property);

        var valueLable = new Label();
        valueLable.style.paddingLeft = 20;

        container.Add(objectField);
        container.Add(valueLable);

        objectField.RegisterValueChangedCallback(evt =>
        {
            var intVariable = evt.newValue as IntVariable;
            if (intVariable != null)
            {
                valueLable.text = $"Value: {intVariable.Value}";
                intVariable.OnValueChanged += newValue => valueLable.text = $"Value: {newValue}";
            }
            else
            {
                valueLable.text = string.Empty;
            }
        });

        var currentIntVariable = objectField.value as IntVariable;

        if (currentIntVariable != null)
        {
            valueLable.text = $"Value: {currentIntVariable.Value}";
            currentIntVariable.OnValueChanged += newValue => valueLable.text = $"Value: {newValue}";
        }

        return container;
    }
}
