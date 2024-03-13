using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace DeiveEx.GameplayTagSystem.Editor
{
    //Nice tutorial on how to use the "experimental" SearchWindow by GameDevGuide:
    //https://www.youtube.com/watch?v=0HHeIUGsuW8
    public class GameplayTagListSearchProvider : ScriptableObject, ISearchWindowProvider
    {
        private GameplayTagContainer _tagContainer;
        private Action<GameplayTag> _onTagSelected;

        public void Setup(GameplayTagContainer container, Action<GameplayTag> onTagSelected)
        {
            _tagContainer = container;
            _onTagSelected = onTagSelected;
        }
        
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            //Entries in the search window are organized based on their position in the list and the "level" property.
            //For example, if we have (a,1) where "a" is the entry and "1" is the level, the list:
            //[(a,1), (b,2)]
            //Will show "b" as a child of "a", and the list:
            //[(a,1), (b,1), (c,2)]
            //will show "a" and "b" as siblings, and "c" as a child of "b"
            var searchList = new List<SearchTreeEntry>();

            //The item with index 0 is the title of the current list
            searchList.Add(new SearchTreeGroupEntry(new GUIContent("Tags"), 0));
            
            //We can use "EditorGUIUtility.IconContent" to get a GUIContent containing a built-in icon from unity!
            //We just need to pass the icon name as a parameter. We can find a list of available icons here along with
            //some neat tools to visualize the icons in the comments:
            //https://gist.github.com/MattRix/c1f7840ae2419d8eb2ec0695448d4321
            var addIconTexture = EditorGUIUtility.IconContent("d_ol_plus").image;

            foreach (var gameplayTag in _tagContainer.GetGameplayTagRecursive())
            {   
                //We add a group entry for every single item, even if they don't have child tags
                var groupEntry = new SearchTreeGroupEntry(new GUIContent(gameplayTag.TagName))
                {
                    level = gameplayTag.FullTagName.Split(".").Length,
                };

                searchList.Add(groupEntry);
                
                //And then we add a normal entry so we can add the tag at any level (also we can only actually select leaf items)
                var setEntry = new SearchTreeEntry(new GUIContent($"Set [{gameplayTag.FullTagName}]", addIconTexture))
                {
                    level = gameplayTag.FullTagName.Split(".").Length + 1, //We need to add this entry as a child of the previous item
                    userData = gameplayTag //We can set a custom object to pass data
                };

                searchList.Add(setEntry);
            }
            
            return searchList;
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            var tag = searchTreeEntry.userData as GameplayTag;
            _onTagSelected(tag);
            return true;
        }
    }
}
