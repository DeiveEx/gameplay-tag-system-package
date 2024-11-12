using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using PopupWindow = UnityEditor.PopupWindow;

namespace DeiveEx.GameplayTagSystem.Editor
{
    public class GameplayTagsEditorWindow : EditorWindow
    {
        #region SubTypes

        private class TagMenuItem
        {
            public GameplayTag tag;
            public VisualElement element;
            public VisualElement parentElement;
            public Foldout foldout;
            public ScrollView scrollView;
        }

        #endregion

        #region Fields

        [SerializeField] private VisualTreeAsset _noDatabasePanel;
        [SerializeField] private VisualTreeAsset _validDatabasePanel;
        [SerializeField] private VisualTreeAsset _tagContainerEntry;
        [SerializeField] private VisualTreeAsset _newTagPopupAsset;

        private Dictionary<string, TagMenuItem> _allEntries = new();
        private ScrollView _mainScrollView;
        private ToolbarSearchField _searchInput;

        #endregion
        
        #region Properties

        private string TagDatabasePath => Path.Combine(GameplayTagDatabase.DatabasePath, $"TagDatabase{GameplayTagDatabase.TAG_DATABASE_FILE_EXTENSION}");
        
        #endregion
        
        #region Unity Events

        [MenuItem("Tools/Edit Gameplay Tags")]
        public static void ShowWindow()
        {
            GameplayTagsEditorWindow wnd = GetWindow<GameplayTagsEditorWindow>();
            wnd.titleContent = new GUIContent("Gameplay Tags");
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();

            LoadDatabaseAsset();

            if (!GameplayTagDatabase.GetAvailableTabFilesInProject().Any())
            {
                DrawNoDatabasePanel();
                return;
            }

            DrawTagDatabasePanel();
        }

        private void OnFocus()
        {
            LoadDatabaseAsset();

            if (!GameplayTagDatabase.Database.CurrentTags.Any())
            {
                CreateGUI();
                return;
            }

            UpdateTagScrollList();
        }

        #endregion

        #region UI Methods

        private void DrawNoDatabasePanel()
        {
            rootVisualElement.Add(_noDatabasePanel.Instantiate());

            var createDatabaseButton = rootVisualElement.Q<Button>("create-database-button");
            createDatabaseButton.clicked += () =>
            {
                if (!Directory.Exists(GameplayTagDatabase.DatabasePath))
                    Directory.CreateDirectory(GameplayTagDatabase.DatabasePath);
                
                File.WriteAllText(TagDatabasePath, "");

                GameplayTagDatabase.LoadDatabasesFromFiles(new[] {TagDatabasePath});
                
                Debug.Log("Tag Database loaded.");
                Debug.Log(GameplayTagDatabase.GetDebugInfo());
                
                CreateGUI();
            };
        }

        private void DrawTagDatabasePanel()
        {
            //IMPORTANT: here we're using "CloneTree" because is we use "Instantiate" we can't define a parent to our elements,
            //so Unity will create a object called "TemplateContainer" to work as a parent, which can mess up the layout of the
            //child objects. I don't know why "Instantiate" doesn't have the option for setting up a parent, but CloneTree does
            _validDatabasePanel.CloneTree(rootVisualElement);

            _mainScrollView = rootVisualElement.Q<ScrollView>("tag-list-scroll");
            var expandButton = rootVisualElement.Q<Button>("expand-all-button");
            var collapseButton = rootVisualElement.Q<Button>("collapse-all-button");
            var addRootTagButton = rootVisualElement.Q<Button>("add-root-tag");

            expandButton.clicked += () =>
            {
                foreach (var tag in GetOrderedTagList())
                {
                    ToggleAllRecursive(_allEntries[tag.FullTagName], true);
                }
            };

            collapseButton.clicked += () =>
            {
                foreach (var tag in GetOrderedTagList())
                {
                    ToggleAllRecursive(_allEntries[tag.FullTagName], false);
                }
            };

            addRootTagButton.clicked += () =>
            {
                var newTagPopup = new TagNamePopup();
                newTagPopup.Setup(_newTagPopupAsset, "New tag:", newTagName => AddNewTag(newTagName));
                PopupWindow.Show(addRootTagButton.worldBound, newTagPopup);
            };

            _searchInput = rootVisualElement.Q<ToolbarSearchField>("search-input");
            _searchInput.RegisterValueChangedCallback(value =>
            {
                FilterAll();
            });

            UpdateTagScrollList();
        }

        private void UpdateTagScrollList()
        {
            if (_mainScrollView == null)
                return;

            _mainScrollView.Clear();

            var oldEntries = _allEntries.ToDictionary(x => x.Key, y => y.Value);
            _allEntries.Clear();

            PopulateScrollView(_mainScrollView, GetOrderedTagList());

            //Open entries that were already open
            foreach (var entry in _allEntries)
            {
                if (oldEntries.TryGetValue(entry.Key, out var oldEntry))
                {
                    entry.Value.foldout.value = oldEntry.foldout.value;
                }
            }

            //Reapply filter
            FilterAll();
        }

        private void PopulateScrollView(ScrollView scrollView, IList<GameplayTag> tagList)
        {
            for (int i = 0; i < tagList.Count; i++)
            {
                CreateNewMenuEntry(scrollView, tagList[i]);
            }
        }
        
        private void CreateNewMenuEntry(ScrollView scrollView, GameplayTag tag)
        {
            var entry = new TagMenuItem()
            {
                tag = tag,
                element = _tagContainerEntry.Instantiate(),
                parentElement = scrollView
            };

            scrollView.Add(entry.element);
            _allEntries.Add(entry.tag.FullTagName, entry);

            PopulateEntry(entry);
        }

        private void PopulateEntry(TagMenuItem entry)
        {
            //Elements
            var label = entry.element.Q<Label>("tag-name");
            var addChildButton = entry.element.Q<Button>("add-child-button");
            var optionsButton = entry.element.Q<Button>("options-button");
            entry.scrollView = entry.element.Q<ScrollView>("child-list");
            entry.foldout = entry.element.Q<Foldout>("tag-fold");

            //Label
            label.text = entry.tag.TagName;

            //Buttons
            addChildButton.clicked += () =>
            {
                var newTagPopup = new TagNamePopup();
                newTagPopup.Setup(_newTagPopupAsset, "New child tag:", newTagName => AddNewTag(newTagName, entry));
                PopupWindow.Show(addChildButton.worldBound, newTagPopup);
            };

            optionsButton.clicked += () =>
            {
                ShowOptionsForEntry(entry);
            };

            //Foldout
            bool hasChildren = entry.tag.ChildTags.Any();

            entry.foldout.value = !hasChildren;
            entry.foldout.style.visibility = hasChildren ? Visibility.Visible : Visibility.Hidden;

            //Children
            ToggleChildList(entry, entry.foldout.value);

            if (hasChildren)
            {
                entry.foldout.RegisterValueChangedCallback(value =>
                {
                    ToggleChildList(entry, entry.foldout.value);
                });

                var childList = GetOrderedTagList(entry.tag);
                PopulateScrollView(entry.scrollView, childList);
            }
        }
        
        private void FilterAll()
        {
            foreach (var entry in _allEntries.Values)
            {
                FilterAllRecursive(entry);
            }
        }

        private bool FilterAllRecursive(TagMenuItem entry)
        {
            bool shouldEnable = entry.tag.TagName.Contains(_searchInput.value);
            bool isAnyChildEnabled = false;

            if (entry.tag.ChildTags.Any())
            {
                foreach (var childTag in entry.tag.ChildTags)
                {
                    var childEntry = _allEntries[childTag.FullTagName];

                    if (FilterAllRecursive(childEntry))
                        isAnyChildEnabled = true;
                }
            }

            entry.element.style.display = shouldEnable || isAnyChildEnabled ? DisplayStyle.Flex : DisplayStyle.None;
            return shouldEnable || isAnyChildEnabled;
        }
        
        private void ToggleChildList(TagMenuItem entry, bool shouldShow)
        {
            entry.scrollView.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ToggleAllRecursive(TagMenuItem entry, bool expand)
        {
            entry.foldout.value = expand;

            if (!entry.tag.ChildTags.Any())
                return;

            foreach (var childEntry in entry.tag.ChildTags)
            {
                ToggleAllRecursive(_allEntries[childEntry.FullTagName], expand);
            }
        }
        
        private void ShowOptionsForEntry(TagMenuItem entry)
        {
            //There's no replacement for Generic menu on UI toolkit as of now
            GenericMenu entryOptions = new GenericMenu();

            entryOptions.AddItem(new GUIContent("Rename Tag"), false, () => RenameTag(entry));
            entryOptions.AddSeparator("");
            entryOptions.AddItem(new GUIContent("Remove Tag"), false, () => ShowRemoveTagDialog(entry));

            entryOptions.ShowAsContext();
        }

        private void ShowRemoveTagDialog(TagMenuItem entry)
        {
            var allTagsToRemove = GameplayTagDatabase.Database.GetGameplayTagRecursive(entry.tag).Select(x => x.FullTagName).ToList();
            string extraCountText = "";

            if (allTagsToRemove.Count > 10)
            {
                int extraCount = 0;

                while (allTagsToRemove.Count > 10)
                {
                    extraCount++;
                    allTagsToRemove.RemoveAt(allTagsToRemove.Count - 1);
                }

                extraCountText = $"\n\n... And {extraCount} more.";
            }

            if (EditorUtility.DisplayDialog("Remove Tag?",
                                            $"Are you sure you want to remove the following tags:\n\n{string.Join("\n", allTagsToRemove)}{extraCountText}",
                                            "Yes",
                                            "No"))
            {
                RemoveTag(entry);
            }
        }

        #endregion

        #region File Methods

        private void LoadDatabaseAsset()
        {
            if (!File.Exists(TagDatabasePath))
                return;
            
            GameplayTagDatabase.LoadDatabasesFromFiles(new[] {TagDatabasePath});
        }

        private void UpdateDatabase()
        {
            if (!File.Exists(TagDatabasePath))
            {
                Debug.LogError("Database file does not exist!");
                return;
            }

            var tagList = new List<string>();

            foreach (var tag in GameplayTagDatabase.Database.GetGameplayTagRecursive())
            {
                //Add only leaf tags
                if (!tag.ChildTags.Any())
                    tagList.Add(tag.FullTagName);
            }

            File.WriteAllLines(TagDatabasePath, tagList.OrderBy(x => x));
        }

        #endregion

        #region Tag Methods
        
        private void AddNewTag(string newTagName, TagMenuItem parentEntry = null)
        {
            string fullTagName = parentEntry == null ? newTagName : $"{parentEntry.tag.FullTagName}.{newTagName}";

            if (GameplayTagDatabase.Database.HasTag(fullTagName))
            {
                Debug.LogError($"Tag [{fullTagName}] already exists");
                return;
            }

            string tagString = parentEntry == null ? "" : parentEntry.tag.FullTagName + ".";
            tagString += newTagName;
            
            GameplayTagDatabase.Database.AddTagInternal(tagString);

            if (parentEntry != null && !parentEntry.foldout.value)
                parentEntry.foldout.value = true;

            UpdateDatabase();
            UpdateTagScrollList();
        }
        
        private void RenameTag(TagMenuItem entry)
        {
            var renamePopup = new TagNamePopup();
            renamePopup.Setup(_newTagPopupAsset, $"Rename tag [{entry.tag.TagName}] to:", newTagName =>
            {
                string newFullName = entry.tag.FullTagName.Replace(entry.tag.TagName, newTagName);

                if (GameplayTagDatabase.Database.HasTag(newFullName))
                {
                    Debug.LogError($"Tag [{newFullName}] already exists");
                    return;
                }

                entry.tag.ChangeTagName(newTagName);
                UpdateDatabase();
                UpdateTagScrollList();
            });

            PopupWindow.Show(entry.element.worldBound, renamePopup);
        }
        
        private void RemoveTag(TagMenuItem entry)
        {
            GameplayTagDatabase.Database.RemoveTagInternal(entry.tag.FullTagName, true);
            entry.parentElement.Remove(entry.element);

            UpdateDatabase();
            UpdateTagScrollList();
        }
        
        #endregion

        #region Helper Methods

        private IList<GameplayTag> GetOrderedTagList(GameplayTag parentTag = null)
        {
            return GameplayTagDatabase.Database.GetRootOrChildList(parentTag).OrderBy(x => x.TagName).ToList();
        }

        #endregion
    }
}