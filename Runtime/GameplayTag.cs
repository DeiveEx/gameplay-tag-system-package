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
		private int _depth;

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

		public int Depth => _depth - 1; //Remove 1 because we don't consider the as a valid depth

		[Tooltip("The number of times this tag was applied. A tag is only removed when this value reaches zero")]
		public int Count => _count;
		public IEnumerable<GameplayTag> ChildTags => _childTags.Values;

		public GameplayTag(string tagName)
		{
			TagName = tagName;
			_count = 1;
		}
		
		/// <summary>
		/// Check if this tag is similar to the given tag. Note that they don't need to be exact the same.
		/// For checking hierarchical exactness, use <see cref="CompareExact"/> 
		/// </summary>
		/// <example>
		/// a.b.c == a.b.c TRUE<br/>
		/// a.b == a.b.c TRUE<br/>
		/// a.b.x == a.b.y FALSE<br/>
		/// </example>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Compare(GameplayTag other)
		{
			//A will be the tag with the smallest depth
			var a = _depth <= other._depth ? this : other;
			var b = _depth > other._depth ? this : other;

			//Get the same depth on B
			while (b._depth != a._depth)
			{
				b = b.ParentTag;
			}
			
			//From the smallest depth and up, the entire hierarchy needs to be the same for it to be considered similar
			for (int i = a.Depth; i >= 0; i--)
			{	
				if (a.TagName != b.TagName)
					return false;

				a = a.ParentTag;
				b = b.ParentTag;
			}

			return true;
		}

		/// <summary>
		/// Check if both tags are exactly the same.
		/// </summary>
		/// <example>
		/// a.b.c == a.b.c TRUE<br/>
		/// a.b.c == a.b FALSE
		/// </example>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool CompareExact(GameplayTag other)
		{
			return FullTagName == other.FullTagName;
		}

		internal void AddChild(GameplayTag tag)
		{
			_childTags.Add(tag.TagName, tag);
			tag._parentTag = this;
			tag._depth = _depth + 1;
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

		public static GameplayTag Create(string tag)
		{
			tag = tag.ToLower();
			string[] tagHierarchy = tag.Split('.');
			var currentTag = new GameplayTag(tagHierarchy[0]);

			for (int i = 1; i < tagHierarchy.Length; i++)
			{
				var childTag = new GameplayTag(tagHierarchy[i]);
				currentTag.AddChild(childTag);
				currentTag = childTag;
			}

			return currentTag;
		}
	}
}
