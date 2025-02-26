using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

//This property makes it so all internal methods defined in this assembly namespace are also available to another assembly.
//Useful for Unit Tests and Editor Tools
[assembly:InternalsVisibleTo("DeiveEx.GameplayTagSystem.EditorTests")]
[assembly:InternalsVisibleTo("DeiveEx.GameplayTagSystem.Editor")]

namespace DeiveEx.GameplayTagSystem
{
	public enum GameplayTagChangedEventType
	{
		Added,
		Removed,
		CounterIncreased,
		CounterDecreased
	}

	public class GameplayTagChangedEventArgs : EventArgs
	{
		public GameplayTag tag;
		public GameplayTagChangedEventType eventType;
	}

	[Serializable]
	public class GameplayTagContainer
	{
		#region Fields

		private List<GameplayTag> _rootTags = new();
		
		#endregion

		#region Properties

		public IEnumerable<GameplayTag> CurrentTags => GetGameplayTagRecursive();

		#endregion

		#region Events & Delegates

		public event EventHandler<GameplayTagChangedEventArgs> tagChanged;

		#endregion
		
		#region Public Methods

		/// <summary>
		/// Adds a tag to the hierarchy or increases its counter, based on whether the tag already exists or not.
		/// </summary>
		/// <param name="tag">The tag to be added/increased.</param>
		public void AddTag(string tag)
		{
			tag = tag.ToLower();
			GameplayTag.ValidateTag(tag);
			AddTagInternal(tag);
		}

		/// <summary>
		/// Either removes the tag or decreases the tags counter from the hierarchy, based on whether the counter is greater than 1.
		/// </summary>
		/// <param name="tag">The tag to be removed.</param>
		/// <param name="ignoreCount">If true, completely removes the tag, no matter the Tag counter</param>
		public void RemoveTag(string tag, bool ignoreCount = false)
		{
			tag = tag.ToLower();
			GameplayTag.ValidateTag(tag);
			RemoveTagInternal(tag, ignoreCount);
		}

		/// <summary>
		/// Check if a certain tag exists in the hierarchy. This returns true even when the tag is not an exact match.<br/>
		/// Ex:<br/>
		/// A.B.C == A.B.C -> true<br/>
		/// A.B == A.B.C -> true
		/// </summary>
		/// <param name="tag">The tag to be searched for.</param>
		/// <returns>Returns true if the tag was found.</returns>
		public bool HasTag(string tag)
		{
			tag = tag.ToLower();
			return GetGameplayTag(tag) != null;
		}

		/// <summary>
		/// Check if the tag exists and if it's an exact match.<br/><br/>
		/// Ex:<br/>
		/// A.B.C == A.B.C -> true<br/>
		/// A.B == A.B.C -> false
		/// </summary>
		/// <param name="tag">The tag to be searched for.</param>
		/// <returns>Returns true if an exact match was found.</returns>
		public bool HasTagExact(string tag)
		{
			tag = tag.ToLower();
			GameplayTag container = GetGameplayTag(tag);
			return container != null && !container.ChildTags.Any();
		}

		/// <summary>
		/// Finds and returns the <see cref="GameplayTag"/> object, if it exists.
		/// </summary>
		/// <param name="tag">The tag to be searched for.</param>
		/// <returns>The <see cref="GameplayTag"/> object equivalent to the tag given. Returns Null if the tag could not be found.</returns>
		public GameplayTag GetGameplayTag(string tag)
		{
			tag = tag.ToLower();
			int[] tagHierarchyHash = tag.Split('.').Select(x => x.GetHashCode()).ToArray();
			int currentDepth = 0;

			IEnumerable<GameplayTag> tagList = _rootTags;
			GameplayTag currentContainer = null;

			do
			{
				currentContainer = GetTagInList(tagHierarchyHash[currentDepth], tagList);

				//If we couldn't find a match for the current child tag, we don't have this tag at all
				if (currentContainer == null)
					break;

				//If we did find a match, we increase the depth and set the new TagList to be searched to be the current tagContainer child list
				currentDepth++;
				tagList = currentContainer.ChildTags;
			}
			while (currentDepth < tagHierarchyHash.Length);

			//If we reached the end of the hierarchy and we have a TagContainer, that means we found our match, else we don't have this tag
			return currentContainer;
		}

		/// <summary>
		/// Check if any of given the tags exists in the current hierarchy of tags.
		/// </summary>
		/// <param name="tagsToSearch">A list of tags to be searched in the hierarchy.</param>
		/// <returns>True if at least one match was found. False otherwise.</returns>
		public bool HasAnyTag(IEnumerable<string> tagsToSearch)
		{
			return tagsToSearch.Any(HasTag);
		}

		/// <inheritdoc cref="HasAnyTag(IEnumerable{string})"/>
		public bool HasAnyTag(params string[] tagsToSearch)
		{
			if (tagsToSearch.Length == 0)
				return _rootTags.Any();
			
			return HasAnyTag((IEnumerable<string>)tagsToSearch);
		}

		/// <summary>
		/// Check if all given tags exists in the current hierarchy
		/// </summary>
		/// <param name="tagsToSearch">A list of tags to be searched in the hierarchy</param>
		/// <returns>True if all tags exists. False otherwise</returns>
		public bool HasAllTags(IEnumerable<string> tagsToSearch)
		{
			return tagsToSearch.All(HasTag);
		}

		/// <inheritdoc cref="HasAllTags(IEnumerable{string})"/>
		public bool HasAllTags(params string[] tagsToSearch)
		{
			return tagsToSearch.Length != 0 &&
			       HasAllTags((IEnumerable<string>)tagsToSearch);
		}

		/// <summary>
		/// Removes all tags from this container
		/// </summary>
		public void ClearTags()
		{
			var childTags = _rootTags.ToList();

			foreach (var tag in childTags)
			{
				_rootTags.Remove(tag);
			}
		}
		
		public string GetDebugInfo()
		{
			StringBuilder sb = new StringBuilder("= [TAGS]");
			sb.Append("\n");
			
			foreach (var tag in GetGameplayTagRecursive())
			{
				//Only add leaf tags to the debug info
				if(tag.ChildTags.Any())
					continue;
				
				sb.Append($"- {tag.FullTagName}");

				if (tag.Count > 1)
					sb.Append($": {tag.Count}");

				sb.Append("\n");
			}

			return sb.ToString();
		}

		public GameplayTagContainerState GetState()
		{
			var tagList = new List<GameplayTagWrapper>();
			var currentTags = GetGameplayTagRecursive();

			foreach (var childTag in currentTags)
			{
				tagList.Add(new GameplayTagWrapper()
				{
					TagName = childTag.TagName,
					ParentTagIndex = childTag.ParentTag  == null ? -1 : currentTags.IndexOf(childTag.ParentTag),
					Count = childTag.Count
				});
			}

			return new GameplayTagContainerState()
			{
				TagCount = tagList.Count,
				Tags = tagList.ToArray(),
			};
		}

		public void ApplyState(GameplayTagContainerState state, bool fireEvents = true)
		{
			string GetFullTagNameFromTagWrapper(GameplayTagWrapper tagWrapper)
			{
				if (tagWrapper.ParentTagIndex > -1)
					return GetFullTagNameFromTagWrapper(state.Tags[tagWrapper.ParentTagIndex]) + "." + tagWrapper.TagName;
				
				return tagWrapper.TagName;
			}
			
			Dictionary<string, GameplayTagWrapper> stateTags = state.Tags.Reverse().ToDictionary(GetFullTagNameFromTagWrapper, x => x);
			var currentTagList = GetGameplayTagRecursive();
			currentTagList.Reverse();
			Dictionary<string, GameplayTag> currentTags = currentTagList.ToDictionary(x => x.FullTagName, x => x);
			
			//Did we remove any tags?
			var removedTags = currentTags.Keys.Where(x => !stateTags.ContainsKey(x));

			foreach (var tag in removedTags)
			{
				if(!HasTagExact(tag))
					continue;
				
				RemoveTagInternal(tag, true, fireEvents);
			}
			
			//Did we add any tags?
			var addedTags = stateTags.Keys.Where(x => !currentTags.ContainsKey(x));

			foreach (var tag in addedTags)
			{	
				if(HasTag(tag))
					continue;
				
				AddTagInternal(tag, fireEvents);
			}
			
			//Did the count changed for any of the remaining tags?
			var modifiedTags = currentTags.Where(x => stateTags.ContainsKey(x.Key) && x.Value.Count != stateTags[x.Key].Count);

			foreach (var tagPair in modifiedTags)
			{
				var stateTag = stateTags[tagPair.Key];
				var actualTag = tagPair.Value;

				var tagChangedEventType = stateTag.Count > actualTag.Count ? GameplayTagChangedEventType.CounterIncreased : GameplayTagChangedEventType.CounterDecreased;
				
				actualTag.SetCount(stateTag.Count);
				
				if(fireEvents)
					tagChanged?.Invoke(this, new GameplayTagChangedEventArgs() { tag = actualTag, eventType = tagChangedEventType });
			}
		}

		#endregion

		#region Private Methods

		private GameplayTag GetTagInList(int tagToSearchHash, IEnumerable<GameplayTag> tagList)
		{
			foreach (var gameplayTag in tagList)
			{
				if (gameplayTag.Hash == tagToSearchHash)
					return gameplayTag;
			}

			return null;
		}

		private GameplayTag GetRootGameplayTag(GameplayTag leafTag)
		{
			var current = leafTag;
			
			while (current.ParentTag != null)
			{
				current = current.ParentTag;
			}

			return current;
		}

		#endregion
		
		#region Internal Methods

		internal void AddTagInternal(string tag, bool fireEvents = true)
		{
			tag = tag.ToLower();
			var gameplayTag = GameplayTag.Create(tag);
			var current = GetRootGameplayTag(gameplayTag);
			GameplayTag currentOwned = null;
			
			//Increase the counter if we already have this tag
			do
			{
				var childList = GetRootOrChildList(currentOwned);
				var existingTag = GetTagInList(current.Hash, childList);

				if (existingTag == null)
					break;
				
				existingTag.SetCount(existingTag.Count + 1);

				if (fireEvents)
					tagChanged?.Invoke(this, new GameplayTagChangedEventArgs() { tag = existingTag, eventType = GameplayTagChangedEventType.CounterIncreased });
					
				current = current.ChildTags.FirstOrDefault(); //Since this is a newly created tag, it only has 1 child in all levels
				currentOwned = existingTag;
			}
			while (current != null);

			//Check if we still have tags to add, which basically means we don't have this tag yet
			if (current == null)
				return;
			
			//If we couldn't find ANY of the tags in the given hierarchy, currentOwned will be null, meaning we need to add this tag to the root
			if(currentOwned == null)
				_rootTags.Add(current);
			else
				currentOwned.AddChild(current);
					
			//Since we're adding all children tags, we need to fire the event for each of them
			if (fireEvents)
			{
				while (current != null)
				{
					tagChanged?.Invoke(this, new GameplayTagChangedEventArgs() { tag = current, eventType = GameplayTagChangedEventType.Added });
					current = current.ChildTags.FirstOrDefault();
				}
			}


			// tag = tag.ToLower();
			// GameplayTag tagContainer = GetGameplayTag(tag);
			//
			// //If we already have this tag, we increase the counter of the entire hierarchy and then return
			// if (tagContainer != null)
			// {
			// 	GameplayTag currentTag = tagContainer;
			//
			// 	do
			// 	{
			// 		currentTag.SetCount(currentTag.Count + 1);
			// 		currentTag = currentTag.ParentTag;
			// 	}
			// 	while (currentTag != null);
			//
			// 	if(fireEvents)
			// 		tagChanged?.Invoke(this, new GameplayTagChangedEventArgs() { tag = tagContainer, eventType = GameplayTagChangedEventType.CounterIncreased });
			// 	
			// 	return;
			// }
			//
			// //If we don't have it yet, we add all nodes of the tag
			// string[] tagHierarchy = tag.Split('.');
			// GameplayTag parentContainer = _masterContainerTag;
			// IEnumerable<GameplayTag> tagList = _masterContainerTag.ChildTags;
			//
			// for (int i = 0; i < tagHierarchy.Length; i++)
			// {
			// 	GameplayTag container = GetTagInList(tagList, tagHierarchy[i]);
			//
			// 	if (container != null)
			// 	{
			// 		container.SetCount(container.Count + 1);
			// 	}
			// 	else
			// 	{
			// 		container = new GameplayTag(tagHierarchy[i]);
			// 		parentContainer.AddChild(container);
			// 		
			// 		if(fireEvents)
			// 			tagChanged?.Invoke(this, new GameplayTagChangedEventArgs() { tag = container, eventType = GameplayTagChangedEventType.Added });
			// 	}
			//
			// 	parentContainer = container;
			// 	tagList = container.ChildTags;
			// }
		}
		
		internal void RemoveTagInternal(string tag, bool ignoreCount = false, bool fireEvents = true)
		{
			tag = tag.ToLower();
			
			//Check if we have this tag
			GameplayTag currentTag = GetGameplayTag(tag);
			GameplayTag parentTag = currentTag?.ParentTag;
			
			if(currentTag == null)
				return;
			
			//Here, we decrease the counter of the entire hierarchy. If the counter reaches zero, we remove it
			do
			{
				//If ignoreCount is true, we set the counter to zero. Otherwise, we only decrease it by 1
				if (ignoreCount && currentTag.FullTagName == tag)
					currentTag.SetCount(0);
				else
					currentTag.SetCount(currentTag.Count - 1);
			
				//If the counter of the tag is zero, we remove it entirely
				if (currentTag.Count <= 0)
				{
					if (parentTag != null)
						parentTag.RemoveChild(currentTag);
					else
						_rootTags.Remove(currentTag);
					
					if(fireEvents)
						tagChanged?.Invoke(this, new GameplayTagChangedEventArgs() { tag = currentTag, eventType = GameplayTagChangedEventType.Removed });
				}
				else
				{
					if(fireEvents)
						tagChanged?.Invoke(this, new GameplayTagChangedEventArgs() { tag = currentTag, eventType = GameplayTagChangedEventType.CounterDecreased });
				}
			
				currentTag = currentTag.ParentTag;
				parentTag = currentTag?.ParentTag;
			}
			while (currentTag != null);
		}
		
		internal IEnumerable<GameplayTag> GetRootOrChildList(GameplayTag gameplayTag = null)
		{
			return gameplayTag == null ? _rootTags : gameplayTag.ChildTags;
		}
		
		internal List<GameplayTag> GetGameplayTagRecursive(GameplayTag parentTag = null)
		{
			List<GameplayTag> tags = new();
			var tagList = GetRootOrChildList(parentTag);

			if(parentTag != null)
				tags.Add(parentTag);

			foreach (var childTag in tagList)
			{
				tags.AddRange(GetGameplayTagRecursive(childTag));
			}
			
			return tags;
		}
		
		#endregion
	}
}
