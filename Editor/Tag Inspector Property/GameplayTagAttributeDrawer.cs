using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace DeiveEx.GameplayTagSystem.Editor
{
    [CustomPropertyDrawer(typeof(GameplayTagAttribute))]
    public class GameplayTagAttributeDrawer : PropertyDrawer
    {
        #region Public Methods

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            LoadAvailableTags();
            
            position = EditorGUI.PrefixLabel(position, label);
            var currentValue = property.stringValue;

            if (string.IsNullOrEmpty(currentValue) ||
                !GameplayTagDatabase.Database.CurrentTags.Any() ||
                !GameplayTagDatabase.Database.HasTag(currentValue)
                )
            {
                currentValue = $"(Invalid value) {currentValue}";
            }

            float buttonWidth = 21;
            float spacing = 2;
            position.width -= (buttonWidth + spacing) * 2;
            
            if (EditorGUI.DropdownButton(position, new GUIContent(currentValue), FocusType.Keyboard))
            {
                if(!GameplayTagDatabase.Database.CurrentTags.Any())
                    return;

                var provider = ScriptableObject.CreateInstance<GameplayTagListSearchProvider>();
                provider.Setup(GameplayTagDatabase.Database, tag => SelectTag(property, tag.FullTagName));
                SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)), provider);
            }

            position.x += position.width + spacing;
            position.width = buttonWidth;

            if (GUI.Button(position, new GUIContent("*", "Edit Tags")))
            {
                var editorWindow = EditorWindow.GetWindow<GameplayTagsEditorWindow>(false, "Gameplay Tags");
                editorWindow.Show();
            }
            
            position.x += position.width + spacing;
            
            if (GUI.Button(position, new GUIContent("R", "Reload Tag Database")))
            {
                LoadAvailableTags(true);
            }
        }

        #endregion
        
        #region Private Methods

        private void LoadAvailableTags(bool forceReload = false)
        {
            try
            {
                GameplayTagDatabase.LoadDatabasesFromFiles();
            }
            catch (NullReferenceException e)
            {
                Debug.LogError(e);
            }
        }

        private void SelectTag(SerializedProperty property, string selectedTag)
        {
            property.stringValue = selectedTag;
            property.serializedObject.ApplyModifiedProperties();
        }
        
        #endregion
    }
}
