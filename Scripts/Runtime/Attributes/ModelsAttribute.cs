using System;
using System.Reflection;
using DoubTech.ThirdParty.AI.Common.Data;
using UnityEditor;
using UnityEngine;

namespace DoubTech.ThirdParty.AI.Common.Attributes
{
    public class ModelsAttribute : PropertyAttribute
    {
        public string ServerConfigFieldName { get; private set; }

        public ModelsAttribute(string serverConfigFieldName)
        {
            ServerConfigFieldName = serverConfigFieldName;
        }
    }
    
    #if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(ModelsAttribute))]
    public class ModelsDrawer : PropertyDrawer
    {
        private FieldInfo serverConfigField;

        public static FieldInfo GetFieldFromHierarchy(Type targetType, string fieldName)
        {
            FieldInfo field = targetType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                return field;
            }
            else if (targetType.BaseType != null)
            {
                return GetFieldFromHierarchy(targetType.BaseType, fieldName);
            }
            else
            {
                return null;
            }
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ModelsAttribute modelsAttribute = attribute as ModelsAttribute;

            // Get the target object the property belongs to
            object targetObject = property.serializedObject.targetObject;
            Type targetType = targetObject.GetType();

            // Use reflection to find the serverConfig field in the target object
            if (serverConfigField == null)
            {
                serverConfigField = GetFieldFromHierarchy(targetType, modelsAttribute.ServerConfigFieldName);
                EditorGUI.LabelField(position, label.text, "Invalid server config field");
                return;
            }

            ApiConfig serverConfig = serverConfigField.GetValue(targetObject) as ApiConfig;
            if (serverConfig == null || serverConfig.Models == null || serverConfig.Models.Length == 0)
            {
                EditorGUI.TextField(position, label, property.stringValue); // Show text field if no models available
            }
            else
            {
                int currentIndex = Mathf.Max(0, Array.IndexOf(serverConfig.Models, property.stringValue));
                currentIndex = EditorGUI.Popup(position, label.text, currentIndex, serverConfig.Models);
                property.stringValue = serverConfig.Models[currentIndex >= 0 ? currentIndex : 0];
            }
        }
    }

    #endif
}