using System;
using System.Collections.Generic;

namespace DeiveEx.GameplayTagSystem
{
	/// <summary>
	/// A representation of an hierarchical tag. A <see cref="GameplayTag"/> should follow the following format: A.B.C<br/>
	/// There's no limit of how long the chain of tags can be
	/// </summary>
	[Serializable]
	public class GameplayTag
	{
		#region Fields

		private GameplayTag _parentTag;
		private Dictionary<string, GameplayTag> _childTags = new();
		private int _count;
		private int _depth;

		#endregion

		#region Properties

		/// <summary>
		/// This tag name, which ignores the hierarchy
		/// </summary>
		public string TagName { get; private set; }

		/// <summary>
		/// The full tag name, which includes the hierarchy
		/// </summary>
		public string FullTagName
		{
			get
			{
				string parentTagName = null;

				if (ParentTag != null)
					parentTagName = $"{ParentTag.FullTagName}.";
				
				return $"{parentTagName}{TagName}";
			}
		}

		public GameplayTag ParentTag => _parentTag;

		public int Depth => _depth;

		/// <summary>
		/// The number of times this tag was applied. A tag is only removed when this value reaches zero
		/// </summary>
		public int Count => _count;
		
		public IEnumerable<GameplayTag> ChildTags => _childTags.Values;

		#endregion

		#region Constructors

		private GameplayTag(string tagName)
		{
			TagName = tagName;
			_count = 1;
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Check if this tag is similar to the given tag.<br/>
		/// Note that they don't need to be exact the same. For checking hierarchical exactness, use <see cref="CompareExact(GameplayTag)"/> 
		/// </summary>
		/// <example>
		/// a.b.c == a.b.c TRUE<br/>
		/// a.b == a.b.c TRUE<br/>
		/// a.b.x == a.b.y FALSE<br/>
		/// </example>
		/// <param name="other">The tag to compare with</param>
		/// <returns>True if both tags share the same hierarchy as the tag with the smallest depth. False otherwise</returns>
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
			do
			{
				if (a.TagName != b.TagName)
					return false;

				a = a.ParentTag;
				b = b.ParentTag;
			}
			while (a != null);

			return true;
		}
		
		/// <inheritdoc cref="Compare(GameplayTag)"/>
		public bool Compare(string tag)
		{
			return Compare(Create(tag));
		}

		/// <summary>
		/// Check if both tags are exactly the same.
		/// </summary>
		/// <example>
		/// a.b.c == a.b.c TRUE<br/>
		/// a.b.c == a.b FALSE<br/>
		/// a.b.c == a.b.x FALSE
		/// </example>
		/// <param name="other">The tag to compare with</param>
		/// <returns>True if both tags have the exact same hierarchy</returns>
		public bool CompareExact(GameplayTag other)
		{
			if (Depth != other.Depth)
				return false;

			GameplayTag a = this;
			GameplayTag b = other;

			do
			{
				if (a.TagName != b.TagName)
					return false;

				a = a.ParentTag;
				b = b.ParentTag;
			}
			while (a != null);

			return true;
		}

		/// <inheritdoc cref="CompareExact(GameplayTag)"/>
		public bool CompareExact(string tag)
		{
			return CompareExact(Create(tag));
		}

		#endregion

		#region Internal Methods

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

		#endregion

		#region Static Methods

		/// <summary>
		/// Creates a new GameplayTag object from the string tag
		/// </summary>
		/// <param name="tag">The full tag name to create</param>
		/// <returns>The child-most GameplayTag object.</returns>
		/// <example>If you pass tag "a.b.c", this method will return a GameplayTag object with TagName "c", with parent
		/// tag "b" and grandparent tag "a", following the hierarchy:<br/>
		/// a<br/>
		/// -b<br/>
		/// --c
		/// </example>
		public static GameplayTag Create(string tag)
		{
			if (string.IsNullOrEmpty(tag))
				throw new NullReferenceException("Tag cannot be null!");
			
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
		
		/// <summary>
		/// Checks if a Tag currently exists in the Database
		/// </summary>
		/// <param name="tag">The tag to check</param>
		/// <exception cref="NullReferenceException">Throw when Database is not loaded</exception>
		/// <exception cref="InvalidOperationException">Throw when Tag is not in Database</exception>
		public static void ValidateTag(string tag)
		{
			if (GameplayTagDatabase.Database == null)
				throw new NullReferenceException($"No Tag Database detected! Make sure the Database is loaded before trying to validate a Tag");
			
			if (GameplayTagDatabase.Database.HasTag(tag))
				return;

			throw new InvalidOperationException($"Tag [{tag}] is not in the database. Make sure the Tag is in the Database before using it");
		}

		#endregion
	}
}
