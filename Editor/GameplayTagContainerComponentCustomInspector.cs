using System.Linq;
using UnityEditor;

namespace DeiveEx.GameplayTagSystem.Editor
{
    [CustomEditor(typeof(GameplayTagContainerComponent))]
    public class GameplayTagContainerComponentCustomInspector : UnityEditor.Editor
    {
        private GameplayTagContainerComponent _instance;
        private GameplayTagContainer _container;
        private static bool _foldout = true;
        private static bool _showFullTagHierarchy;

        private void OnEnable()
        {
            _instance = (GameplayTagContainerComponent)target;

            _container = _instance.gameObject.GetGameplayTags();
            _container.tagChanged += OnTagChanged;
        }

        private void OnDisable()
        {
            _container.tagChanged -= OnTagChanged;
        }

        private void OnTagChanged(object sender, GameplayTagChangedEventArgs e)
        {
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            //Extending to show extra info
            _foldout = EditorGUILayout.BeginFoldoutHeaderGroup(_foldout, "Inspect Tags");
            
            if (_foldout)
            {
                if (!_container.HasAnyTag())
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.LabelField("No Tags currently added.");
                    }
                else
                    DrawTags();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawTags()
        {
            _showFullTagHierarchy = EditorGUILayout.Toggle("Show full tag hierarchy", _showFullTagHierarchy);

            using (new EditorGUI.DisabledScope(true))
            {
                foreach (var tag in _container.CurrentTags)
                {
                    var gameplayTag = _container.GetGameplayTag(tag);

                    if(!_showFullTagHierarchy && gameplayTag.ChildTags.Any())
                        continue;
                
                    EditorGUILayout.BeginHorizontal();
                    
                    EditorGUILayout.PrefixLabel(tag);
                    EditorGUILayout.LabelField($"{gameplayTag.Count}");
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }
}
