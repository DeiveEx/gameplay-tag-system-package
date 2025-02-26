using NUnit.Framework;
using DeiveEx.GameplayTagSystem;
using UnityEngine;

[Category("GameplayTagSystem")]
public class GameplayTagSystemTests
{
	private GameplayTagContainer _tagContainer;

	//Setup methods are executed before each test
	[SetUp]
	public void Setup()
	{
		_tagContainer = new GameplayTagContainer();
	}

	//TearDown methods are executed after each test
	[TearDown]
	public void TearDown()
	{
	}

	[Test]
	public void TagManager_Created_With_Empty_Tag_List()
	{
		Assert.IsNotNull(_tagContainer);
		Assert.IsFalse(_tagContainer.HasTag("a"));
	}

	[Test]
	[TestCase("a")]
	[TestCase("a.a")]
	[TestCase("a.a.a")]
	[TestCase("a.a.a.a")]
	public void Was_Tag_Correctly_Added(string tag)
	{
		_tagContainer.AddTagInternal(tag);
		Assert.IsTrue(_tagContainer.HasTag(tag));
		Assert.IsTrue(_tagContainer.HasTagExact(tag));
		
		var tagObj = _tagContainer.GetGameplayTag(tag);
		Assert.IsNotNull(tagObj);
		Assert.AreEqual(tagObj.FullTagName, tag);
	}

	[Test]
	[TestCase("a")]
	[TestCase("a", "a.b.c")]
	[TestCase("a", "x", "a.b")]
	public void Any_Tag_Exists(params string[] tagsToSearch)
	{
		for (int i = 0; i < tagsToSearch.Length; i++)
		{
			_tagContainer.AddTagInternal(tagsToSearch[i]);
		}

		Assert.IsTrue(_tagContainer.HasAnyTag(tagsToSearch));

		tagsToSearch = new string[] {
			"asd.asd.asd",
			"q.w.e"
		};
		Assert.IsFalse(_tagContainer.HasAnyTag(tagsToSearch));
		
		tagsToSearch = new string[] {
			"b",
			"a.b"
		};
	}

	[Test]
	[TestCase("a")]
	[TestCase("a", "a.b.c")]
	[TestCase("a", "x", "a.b")]
	public void All_Tag_Exists(params string[] tagsToSearch)
	{
		//Check if the entire list exists
		for (int i = 0; i < tagsToSearch.Length; i++)
		{
			_tagContainer.AddTagInternal(tagsToSearch[i]);
		}

		Assert.IsTrue(_tagContainer.HasAllTags(tagsToSearch));

		//Check if all items in the list exists, even though it's not all items
		var incompleteTags = new string[tagsToSearch.Length - 1];

		for (int i = 0; i < incompleteTags.Length; i++)
		{
			incompleteTags[i] = tagsToSearch[i];
		}

		Assert.IsTrue(_tagContainer.HasAllTags(tagsToSearch));

		//Check if all items in the list exists, with one extra item
		var tagListWithExtra = new string[tagsToSearch.Length + 1];

		for (int i = 0; i < tagsToSearch.Length; i++)
		{
			tagListWithExtra[i] = tagsToSearch[i];
		}

		tagListWithExtra[^1] = "asdasdasd";
		Assert.IsFalse(_tagContainer.HasAllTags(tagListWithExtra));
	}

	[Test]
	[TestCase("a")]
	[TestCase("a.a")]
	[TestCase("a.a.a")]
	[TestCase("a.a.a.a")]
	public void Can_Check_Tags_In_Hierarchy(string tag)
	{
		_tagContainer.AddTagInternal(tag);

		string[] tagHierarchy = tag.Split('.');

		for (int i = 0; i < tagHierarchy.Length; i++)
		{
			Assert.IsTrue(_tagContainer.HasTag(tagHierarchy[i]));
		}
	}

	[Test]
	[TestCase("a")]
	[TestCase("a.a")]
	[TestCase("a.a.a")]
	[TestCase("a.a.a.a")]
	public void Was_Tag_Correctly_Removed(string tag)
	{
		_tagContainer.AddTagInternal(tag);
		Assert.IsTrue(_tagContainer.HasTag(tag));

		_tagContainer.RemoveTagInternal(tag);
		Assert.IsFalse(_tagContainer.HasTag(tag));
	}

	[Test]
	[TestCase("a")]
	[TestCase("a.a")]
	[TestCase("a.a.a")]
	[TestCase("a.a.a.a")]
	public void Tag_Full_Name_Is_Correct(string tag)
	{
		_tagContainer.AddTagInternal(tag);
		GameplayTag container = _tagContainer.GetGameplayTag(tag);

		Assert.AreEqual(container.FullTagName, tag);
	}

	[Test]
	[TestCase("a", "a.b")]
	[TestCase("a.a", "a")]
	[TestCase("a.b", "a")]
	public void Tag_Has_Exact_Name(string exactTag, string wrongTag)
	{
		_tagContainer.AddTagInternal(exactTag);

		Assert.IsTrue(_tagContainer.HasTagExact(exactTag));
		Assert.IsFalse(_tagContainer.HasTagExact(wrongTag));
	}

	[Test]
	[TestCase("a", 1, 0)]
	[TestCase("a", 1, 1)]
	[TestCase("a", 5, 0)]
	[TestCase("a", 10, 2)]
	[TestCase("a", 10, 10)]
	[TestCase("a", 10, 15)]
	[TestCase("a", 0, 1)]
	public void Tag_Counter_Changes_Correctly(string tag, int amountToAdd, int amountToRemove)
	{
		for (int i = 0; i < amountToAdd; i++)
		{
			this._tagContainer.AddTagInternal(tag);
		}

		for (int i = 0; i < amountToRemove; i++)
		{
			this._tagContainer.RemoveTagInternal(tag);
		}

		GameplayTag gameplayTag = _tagContainer.GetGameplayTag(tag);
		int finalAmount = amountToAdd - amountToRemove;

		if (finalAmount <= 0)
		{
			Assert.IsNull(gameplayTag);
		}
		else
		{
			Assert.AreEqual(gameplayTag.Count, finalAmount);
		}
	}

	[Test]
	[TestCase(1, 0)]
	[TestCase(0, 0)]
	[TestCase(1, 1)]
	[TestCase(0, 1)]
	[TestCase(10, 0)]
	[TestCase(10, 5)]
	[TestCase(0, 10)]
	[TestCase(5, 10)]
	public void Tag_Change_Events_Are_raised_Correctly(int amountToAdd, int amountToRemove)
	{
		GameplayTag tag = null;
		int added = 0;
		int removed = 0;
		int increased = 0;
		int decreased = 0;

		_tagContainer.tagChanged += (sender, e) =>
		{
			tag = e.tag;

			switch (e.eventType)
			{
				case GameplayTagChangedEventType.Added:
					added++;
					break;
				case GameplayTagChangedEventType.Removed:
					removed++;
					break;
				case GameplayTagChangedEventType.CounterIncreased:
					increased++;
					break;
				case GameplayTagChangedEventType.CounterDecreased:
					decreased++;
					break;
				default:
					break;
			}
		};

		//Add and remove the tags by the defined amounts
		string testTag = "a";

		for (int i = 0; i < amountToAdd; i++)
		{
			_tagContainer.AddTagInternal(testTag);
		}

		for (int i = 0; i < amountToRemove; i++)
		{
			_tagContainer.RemoveTagInternal(testTag);
		}

		//Check if the tag should still exist
		if (amountToAdd > 0)
		{
			Assert.IsNotNull(tag);
		}

		//Check if the amount of events fired are correct
		int total = amountToAdd > 0 ? 1 : 0; //No matter how many time we add, the added event should only fire once. It'll only fire more than once if we add, remove and then add the same tag again
		Assert.AreEqual(added, total);

		total = Mathf.Max(amountToAdd - 1, 0); //Since the first time we add a tag the added event is fired, we have to subtract 1 from the total amount of times we added the tag
		Assert.AreEqual(increased, total);

		if (amountToAdd > 0)
		{
			total = amountToRemove >= amountToAdd ? 1 : 0; //We'll only have a remove event if the amount to remove is greater or equal to the amount added
		}
		else
		{
			total = 0;
		}

		Assert.AreEqual(removed, total);

		total = amountToRemove;

		//If we are trying to remove the tag more times than we're adding it, the remove events will only get fired as long as we the tag still exists
		if (amountToRemove >= amountToAdd)
		{
			total = Mathf.Max(amountToAdd - 1, 0);
		}

		Assert.AreEqual(decreased, total);
	}

	//Here the tags are hard-coded for this test specifically. Other combinations of tags might not work
	[Test]
	[TestCase("a.b.c", 1, "a.b.c", 0)]
	[TestCase("a.b.c", 5, "a.b.c", 0)]
	[TestCase("a.b.c", 5, "a.b.c", 3)]
	[TestCase("a.b.c", 1, "a.b.c", 1)]
	[TestCase("a.b.c", 5, "a.b.c", 5)]
	[TestCase("a.b.c", 5, "a.b.c", 10)]
	public void Tag_Counter_In_Hierarchy_Is_Correctly_Set(string tagToAdd, int amountToAdd, string tagToRemove, int amountToRemove)
	{
		for (int i = 0; i < amountToAdd; i++)
		{
			_tagContainer.AddTagInternal(tagToAdd);
		}

		for (int i = 0; i < amountToRemove; i++)
		{
			_tagContainer.RemoveTagInternal(tagToRemove);
		}

		var a = _tagContainer.GetGameplayTag("a");
		var b = _tagContainer.GetGameplayTag("a.b");
		var c = _tagContainer.GetGameplayTag("a.b.c");

		if (a != null)
			Assert.AreEqual(amountToAdd - amountToRemove, a.Count);

		if (b != null)
			Assert.AreEqual(amountToAdd - amountToRemove, b.Count);

		if (c != null)
			Assert.AreEqual(amountToAdd - amountToRemove, c.Count);
	}

	[Test]
	public void Tag_Counter_In_Hierarchy_Is_Correctly_Set_More_Complex()
	{
		_tagContainer.AddTagInternal("a");
		_tagContainer.AddTagInternal("a.b");
		_tagContainer.AddTagInternal("a.b.c");
		_tagContainer.AddTagInternal("a.b.c");
		_tagContainer.AddTagInternal("x.y");
		_tagContainer.AddTagInternal("x.y");
		_tagContainer.AddTagInternal("x.y.z");

		var a = _tagContainer.GetGameplayTag("a");
		var b = _tagContainer.GetGameplayTag("a.b");
		var c = _tagContainer.GetGameplayTag("a.b.c");
		var x = _tagContainer.GetGameplayTag("x");
		var y = _tagContainer.GetGameplayTag("x.y");
		var z = _tagContainer.GetGameplayTag("x.y.z");

		Assert.AreEqual(a.Count, 4); //Adding/removing child tags should change the parent counter as well
		Assert.AreEqual(b.Count, 3);
		Assert.AreEqual(c.Count, 2);

		Assert.AreEqual(x.Count, 3);
		Assert.AreEqual(y.Count, 3);
		Assert.AreEqual(z.Count, 1);

		_tagContainer.RemoveTagInternal("a.b.c");

		Assert.AreEqual(a.Count, 3);
		Assert.AreEqual(b.Count, 2);
		Assert.AreEqual(c.Count, 1);

		_tagContainer.RemoveTagInternal("x.y.z");

		Assert.AreEqual(x.Count, 2);
		Assert.AreEqual(y.Count, 2);
		Assert.AreEqual(z.Count, 0); //We still have the reference here, but it should be removed from the container by this point
		Assert.IsFalse(_tagContainer.HasTag("x.y.z")); //And we can confirm that here

		_tagContainer.RemoveTagInternal("a");

		Assert.AreEqual(a.Count, 2);
		Assert.AreEqual(b.Count, 2);
		Assert.AreEqual(c.Count, 1);
	}

	[Test]
	[TestCase("a.b.c", 10, "a.b.c", new string[] { "a.b.c" })]
	[TestCase("a.b.c", 10, "a.b", new string[] { "a.b.c", "a.b" })]
	[TestCase("a.b.c", 10, "a", new string[] { "a.b.c", "a.b", "a" })]
	public void Remove_Tag_Ignore_Counter_Working(string tagToAdd, int amountToAdd, string tagToRemove, string[] tagsThatShouldNotExist)
	{
		for (int i = 0; i < amountToAdd; i++)
		{
			_tagContainer.AddTagInternal(tagToAdd);
		}

		_tagContainer.RemoveTagInternal(tagToRemove, true);

		foreach (var tag in tagsThatShouldNotExist)
		{
			Assert.IsFalse(_tagContainer.HasTag(tag));
		}
	}

	[Test]
	[TestCase("a", "a.b", "a.b")]
	public void Remove_Tag_Ignore_Counter_Only_Removes_Child(string parentTag, string childTag, string tagToRemove)
	{
		_tagContainer.AddTagInternal(parentTag);
		_tagContainer.AddTagInternal(childTag);

		_tagContainer.RemoveTagInternal(tagToRemove, true);
		
		Assert.IsTrue(_tagContainer.HasTag(parentTag));
	}

	[Test]
	[TestCase(new string[] { "a", "a" }, "a", false, "a", 1)]
	[TestCase(new string[] { "a.b", "a.b" }, "a.b", false, "a.b", 1)]
	[TestCase(new string[] { "a", "a.b" }, "a.b", false, "a", 1)]
	[TestCase(new string[] { "a.b.c" }, "a", false, "a", 0)]
	[TestCase(new string[] { "a.b.c" }, "a.b", false, "a", 0)]
	[TestCase(new string[] { "a", "a" }, "a", true, "a", 0)]
	//Since we can't know how tags are added (they can be added one after the other or all at once), the parent count becomes [No of applications - No of removes], which in the case below is [3 application - 1 remove = 2]
	[TestCase(new string[] { "a", "a.b", "a.b.c" }, "a.b", true, "a", 2)]
	[TestCase(new string[] { "a", "a.b", "a.b.c", "a.b.c.d" }, "a.b", true, "a", 3)]
	[TestCase(new string[] { "a", "a.b.c.d" }, "a.b", true, "a", 1)]
	public void Remove_Tag_Counter_Still_Correct(string[] tagsToAdd, string tagToRemove, bool ignoreCounter, string tagToCheckCounter, int expectedCounter)
	{
		foreach (var tag in tagsToAdd)
		{
			_tagContainer.AddTagInternal(tag);
		}
		
		_tagContainer.RemoveTagInternal(tagToRemove, ignoreCounter);

		var gameplayTag = _tagContainer.GetGameplayTag(tagToCheckCounter);
		
		if(expectedCounter == 0)
			Assert.IsNull(gameplayTag);
		else
			Assert.AreEqual(expectedCounter, gameplayTag.Count);
	}

	[Test]
	[TestCase("a", "a")]
	[TestCase("a.b", "a.b")]
	[TestCase("a.b.c", "a.b.c")]
	public void Tag_Exists_On_Correct_Hierarchy_Level(string tagToSearch, string tagToAdd)
	{
		_tagContainer.AddTagInternal(tagToAdd);
		Assert.IsTrue(_tagContainer.HasTag(tagToSearch));
	}
	
	[Test]
	[TestCase("x", "a")]
	[TestCase("x", "a.x")]
	[TestCase("x", "a.b.x")]
	[TestCase("a.x", "a.b.x")]
	public void Tag_Does_Not_Exist_On_Incorrect_Hierarchy_Level(string tagToSearch, string tagToAdd)
	{
		_tagContainer.AddTagInternal(tagToAdd);
		Assert.IsFalse(_tagContainer.HasTag(tagToSearch));
	}

	[Test]
	[TestCase("a")]
	[TestCase("a", "b")]
	[TestCase("a.b", "x.y.z")]
	[TestCase("a.a.a.a.a.a.a")]
	public void Tag_Container_Clears_Correctly(params string[] tagsToAdd)
	{
		foreach (var tag in tagsToAdd)
		{
			_tagContainer.AddTagInternal(tag);
		}
		
		_tagContainer.ClearTags();

		foreach (var tag in tagsToAdd)
		{
			Assert.IsFalse(_tagContainer.HasTag(tag));
		}
	}
	
	[Test]
	[TestCase("a")]
	[TestCase("a.b")]
	[TestCase("a.b.c")]
	public void Tag_Has_Correct_Parent(string tagToAdd)
	{
		_tagContainer.AddTagInternal(tagToAdd);
		var gameplayTag = _tagContainer.GetGameplayTag(tagToAdd);

		string[] parts = tagToAdd.Split(".");

		if (parts.Length == 1)
		{
			Assert.IsNull(gameplayTag.ParentTag);
		}
		else
		{
			Assert.AreEqual(parts[^2], gameplayTag.ParentTag.TagName);
		}
	}
	
	[Test]
	[TestCase("a", 0)]
	[TestCase("a.b", 1)]
	[TestCase("a.b.c", 2)]
	public void Tag_Has_Correct_Depth(string tagToAdd, int expectedDepth)
	{
		_tagContainer.AddTagInternal(tagToAdd);
		var gameplayTag = _tagContainer.GetGameplayTag(tagToAdd);
		
		Assert.AreEqual(expectedDepth, gameplayTag.Depth);
	}
	
	[Test]
	[TestCase("a", "a", true)]
	[TestCase("a.b", "a.b", true)]
	[TestCase("a.b", "x.y", false)]
	[TestCase("a.b", "a.x", false)]
	public void Tag_CompareExact(string tagA, string tagB, bool expectedResult)
	{
		var a = GameplayTag.Create(tagA);
		var b = GameplayTag.Create(tagB);
		
		Assert.AreEqual(expectedResult, a.CompareExact(b));
	}
	
	[Test]
	[TestCase("a", "a", true)]
	[TestCase("a.b.c", "a.b.c", true)]
	[TestCase("a", "a.b", true)]
	[TestCase("a.b", "a", true)]
	[TestCase("a.x", "a.y", false)]
	[TestCase("a.b.x", "a.y", false)]
	public void Tag_Compare(string tagA, string tagB, bool expectedResult)
	{
		var a = GameplayTag.Create(tagA);
		var b = GameplayTag.Create(tagB);
		
		Assert.AreEqual(expectedResult, a.Compare(b));
	}
	
	[Test]
	[TestCase("a")]
	[TestCase("a.b")]
	[TestCase("a.b.c")]
	[TestCase("x.y.z")]
	public void Tag_Created_Successfully(string tag)
	{
		var gameplayTag = GameplayTag.Create(tag);
		Assert.AreEqual(tag, gameplayTag.FullTagName);
	}
}
