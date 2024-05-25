using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeiveEx.GameplayTagSystem
{
	/// <summary>
	/// A representation of an hierarchical tag. A <see cref="GameplayTag"/> should follow the following format: A.B.C<br/>
	/// There's no limit of how long the chain of tags can be
	/// </summary>
	[Serializable]
	public class GameplayTag
	{
		internal const string MASTER_TAG = "MasterTag";

		private GameplayTag _parentTag;
		private Dictionary<string, GameplayTag> _childTags = new();
		private int _count;

		public string TagName { get; private set; }

		public string FullTagName
		{
			get
			{
				string parentTagName = null;

				if (ParentTag != null && ParentTag.TagName != MASTER_TAG)
					parentTagName = $"{ParentTag.FullTagName}.";
				
				return $"{parentTagName}{TagName}";
			}
		}

		public GameplayTag ParentTag
		{
			get
			{
				if (_parentTag.TagName == MASTER_TAG)
					return null;

				return _parentTag;
			}
		}

		[Tooltip("The number of times this tag was applied. A tag is only removed when this value reaches zero")]
		public int Count => _count;
		public IEnumerable<GameplayTag> ChildTags => _childTags.Values;

		public GameplayTag(string tagName)
		{
			TagName = tagName;
			_count = 1;
		}

		internal void AddChild(GameplayTag tag)
		{
			_childTags.Add(tag.TagName, tag);
			tag._parentTag = this;
		}

		internal void RemoveChild(GameplayTag tag)
		{
			_childTags.Remove(tag.TagName);
		}

		internal void SetCount(int value)
		{
			_count = value;

			if (_count < 0)
				_count = 0;
		}

		internal void ChangeTagName(string newName)
		{
			TagName = newName;
		}
		
		internal GameplayTag GetParentTagInternal()
		{
			return _parentTag;
		}
	}
}
