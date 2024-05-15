using UnityEditor;
using UnityEngine;

namespace DoubTech.ThirdParty.AI.Common.Attributes
{
    public class PasswordAttribute : PropertyAttribute
    {
        public PasswordAttribute() { }
    }
    
    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(PasswordAttribute))]
    public class PasswordDrawer : PropertyDrawer
    {
        private bool isPasswordShown = false;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Check if the property is of the correct type (string)
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            int buttonSize = 20;
            Rect fieldRect = new Rect(position.x, position.y, position.width - buttonSize, position.height);
            Rect buttonRect = new Rect(position.x + position.width - buttonSize, position.y, buttonSize, position.height);

            EditorGUI.BeginChangeCheck();
            string value = isPasswordShown ? EditorGUI.TextField(fieldRect, label, property.stringValue) 
                : EditorGUI.PasswordField(fieldRect, label, property.stringValue);

            if (EditorGUI.EndChangeCheck())
            {
                property.stringValue = value;
            }

            isPasswordShown = GUI.Toggle(buttonRect, isPasswordShown, "👁", "button");
        }
    }
    #endif
}