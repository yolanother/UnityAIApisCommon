using System;
using UnityEditor;
using UnityEngine;

namespace DoubTech.ThirdParty.AI.Common.Attributes
{
    public class PopupAttribute : PropertyAttribute
    {
        public string[] Options { get; private set; }

        public PopupAttribute(params string[] options)
        {
            this.Options = options;
        }
    }
    
    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(PopupAttribute))]
    public class PopupDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                PopupAttribute popupAttribute = (PopupAttribute)attribute;

                // Find the index of the current value, or default to the first option if it's not in the list
                int index = Mathf.Max(0, Array.IndexOf(popupAttribute.Options, property.stringValue));

                // Draw the popup box with the provided options
                index = EditorGUI.Popup(position, label.text, index, popupAttribute.Options);

                // Update the property value if an option is selected
                property.stringValue = popupAttribute.Options[index];
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
                EditorGUI.HelpBox(position, $"{property.name} is not a string.", MessageType.Error);
            }
        }
    }
#endif
}