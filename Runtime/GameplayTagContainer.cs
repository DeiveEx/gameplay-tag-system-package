using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

//This property makes it so all internal methods defined in this assembly namespace are also available to another assembly.
//Useful for Unit Tests and Editor Tools
[assembly:InternalsVisibleTo("GameplayTagSystem.EditorTests")]
[assembly:InternalsVisibleTo("GameplayTagSystem.Editor")]

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

		private GameplayTag _masterContainerTag = new(GameplayTag.MASTER_TAG);
		
		#endregion

		#region Properties

		public IEnumerable<string> CurrentTags => GetGameplayTagRecursive().Select(x => x.FullTagName);

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
			ValidateTag(tag);
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
			ValidateTag(tag);
			RemoveTagInternal(tag, ignoreCount);
		}

		/// <summary>
		/// Check if a certain tag exists in the hierarchy.
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
		/// An exact match works as:<br/>
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
			string[] tagHierarchy = tag.Split('.');
			int currentDepth = 0;

			IEnumerable<GameplayTag> tagList = _masterContainerTag.ChildTags;
			GameplayTag currentContainer = null;

			do
			{
				currentContainer = GetTagInList(tagList, tagHierarchy[currentDepth]);

				//If we couldn't find a match for the current child tag, we don't have this tag at all
				if (currentContainer == null)
					break;

				//If we did find a match, we increase the depth and set the new TagList to be searched to be the current tagContainer child list
				currentDepth++;
				tagList = currentContainer.ChildTags;
			}
			while (currentDepth < tagHierarchy.Length);

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
				return _masterContainerTag.ChildTags.Any();
			
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

		public void ValidateTag(string tag)
		{
			if (GameplayTagDatabase.Database == null)
				throw new NullReferenceException($"No Tag Database detected! Make sure the Database is loaded before trying to validate a Tag");
			
			if (GameplayTagDatabase.Database.HasTag(tag))
				return;

			throw new InvalidOperationException($"Tag [{tag}] is not in the database. Make sure the Tag is in the Database before using it");
		}

		/// <summary>
		/// Removes all tags from this container
		/// </summary>
		public void ClearTags()
		{
			var childTags = _masterContainerTag.ChildTags.ToList();

			foreach (var tag in childTags)
			{
				_masterContainerTag.RemoveChild(tag);
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

		#endregion

		#region Private Methods

		private GameplayTag GetTagInList(IEnumerable<GameplayTag> tagList, string childTag)
		{
			foreach (var childContainer in tagList)
			{
				if (childContainer.TagName == childTag)
					return childContainer;
			}

			return null;
		}

		#endregion
		
		#region Internal Methods

		internal void AddTagInternal(string tag)
		{
			tag = tag.ToLower();
			GameplayTag tagContainer = GetGameplayTag(tag);

			//If we already have this tag, we increase the counter of the entire hierarchy and then return
			if (tagContainer != null)
			{
				GameplayTag currentTag = tagContainer;

				do
				{
					currentTag.SetCount(currentTag.Count + 1);
					currentTag = currentTag.ParentTag;
				}
				while (currentTag != null);

				tagChanged?.Invoke(this, new GameplayTagChangedEventArgs() { tag = tagContainer, eventType = GameplayTagChangedEventType.CounterIncreased });
				return;
			}

			//If we don't have it yet, we add all nodes of the tag
			string[] tagHierarchy = tag.Split('.');
			GameplayTag parentContainer = _masterContainerTag;
			IEnumerable<GameplayTag> tagList = _masterContainerTag.ChildTags;

			for (int i = 0; i < tagHierarchy.Length; i++)
			{
				GameplayTag container = GetTagInList(tagList, tagHierarchy[i]);

				if (container != null)
				{
					container.SetCount(container.Count + 1);
				}
				else
				{
					container = new GameplayTag(tagHierarchy[i]);
					parentContainer.AddChild(container);
					tagChanged?.Invoke(this, new GameplayTagChangedEventArgs() { tag = container, eventType = GameplayTagChangedEventType.Added });
				}

				parentContainer = container;
				tagList = container.ChildTags;
			}
		}
		
		internal void RemoveTagInternal(string tag, bool ignoreCount = false)
		{
			tag = tag.ToLower();
			GameplayTag tagContainer = GetGameplayTag(tag);

			//If we don't have this tag, we return
			if (tagContainer == null)
				return;

			//Here, we decrease the counter of the entire hierarchy. If the counter reaches zero, we remove it
			GameplayTag currentTag = tagContainer;
			GameplayTag parentTag = tagContainer.ParentTag;

			do
			{
				//Here we set the counter to zero is we want to ignore the counter. Otherwise, we only decrease its counter
				//Note that we only completely
				if (ignoreCount && currentTag.FullTagName == tag)
					currentTag.SetCount(0);
				else
					currentTag.SetCount(currentTag.Count - 1);

				//If the counter of the tag is zero, we remove it entirely
				if (currentTag.Count <= 0)
				{
					parentTag.RemoveChild(currentTag);
					tagChanged?.Invoke(this, new GameplayTagChangedEventArgs() { tag = currentTag, eventType = GameplayTagChangedEventType.Removed });
				}
				else
				{
					tagChanged?.Invoke(this, new GameplayTagChangedEventArgs() { tag = currentTag, eventType = GameplayTagChangedEventType.CounterDecreased });
				}

				currentTag = currentTag.ParentTag;
				parentTag = currentTag.ParentTag;
			}
			while (currentTag != _masterContainerTag);
		}
		
		internal IEnumerable<GameplayTag> GetChildTagList(GameplayTag parentTag = null)
		{
			if (parentTag == null)
				parentTag = _masterContainerTag;
			
			if(parentTag != _masterContainerTag && !HasTag(parentTag.FullTagName))
				throw new NullReferenceException($"$Tag [{parentTag.FullTagName}] does not exist, so it's not possible to find the child tags for it");
			
			return parentTag.ChildTags;
		}
		
		internal IEnumerable<GameplayTag> GetGameplayTagRecursive(GameplayTag parentTag = null)
		{
			if (parentTag == null)
				parentTag = _masterContainerTag;
			
			List<GameplayTag> tags = new();

			if(parentTag != _masterContainerTag)
				tags.Add(parentTag);

			foreach (var childTag in parentTag.ChildTags)
			{
				tags.AddRange(GetGameplayTagRecursive(childTag));
			}
			
			return tags;
		}
		
		#endregion
	}
}
