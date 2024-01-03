using UnityEditor;
using UnityEditor.UIElements;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;

class BehaviorTreeSettings : ScriptableObject
{
    public const string k_MyCustomSettingsPath = "Assets/Editor/BehaviorTreeSettings.asset";
    [System.Serializable]
    private class NodeSetting
    {
        public MonoScript script;
        public string defaultName = "";
        public Texture2D icon;
    }

    [SerializeField]
    private Texture2D defaultTexture;
    [SerializeField]
    private List<NodeSetting> nodeSettings;


    internal static BehaviorTreeSettings GetOrCreateSettings()
    {
        var settings = AssetDatabase.LoadAssetAtPath<BehaviorTreeSettings>(k_MyCustomSettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<BehaviorTreeSettings>();
            settings.nodeSettings = new List<NodeSetting>();
            AssetDatabase.CreateAsset(settings, k_MyCustomSettingsPath);
            AssetDatabase.SaveAssets();
        }
        return settings;
    }


    internal static SerializedObject GetSerializedSettings()
    {
        return new SerializedObject(GetOrCreateSettings());
    }
}


// Register a SettingsProvider using UIElements for the drawing framework:
static class BehaviorTreeSettingsUIElementsRegister
{
    class BehaviorTreeSettingsProvider : SettingsProvider
    {
        private SerializedObject m_BehaviorTreeSettings;

        const string k_BehaviorTreeSettingsSettingsPath = "Assets/Editor/BehaviorTreeSettings.asset";
        public BehaviorTreeSettingsProvider(string path, SettingsScope scope = SettingsScope.User)
            : base(path, scope) { }

        public static bool IsSettingsAvailable()
        {
            return File.Exists(k_BehaviorTreeSettingsSettingsPath);
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            // This function is called when the user clicks on the MyCustom element in the Settings window.
            m_BehaviorTreeSettings = BehaviorTreeSettings.GetSerializedSettings();
        }

        public override void OnGUI(string searchContext)
        {
            SerializedProperty property = m_BehaviorTreeSettings.GetIterator();
            if (property.NextVisible(true))
            {
                do
                {
                    if (property.name == "m_Script")
                    {
                        continue;
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(property, new GUIContent(property.displayName), true);
                    }

                } while (property.NextVisible(false));
            }

            m_BehaviorTreeSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        // Register the SettingsProvider
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            if (IsSettingsAvailable())
            {
                var provider = new BehaviorTreeSettingsProvider("Project/Behavior Tree Settings", SettingsScope.Project);
                // Automatically extract all keywords from the Styles.

                return provider;
            }

            // Settings Asset doesn't exist yet; no need to display anything in the Settings window.
            return null;
        }
    }
}