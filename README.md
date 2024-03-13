# Description

A tag system based on the Tag System used in the GameplayAbilitySystem for Unreal.

Tags are defined in a tree-like structure, and you can check if an object has a tag on any level of the tree, meaning
that if a character has a tag "State.Stunned.Electric" and you want to check if the character is stunned, you can
simple check for "State.Stunned" instead.

Tags also have a concept of "stacking", meaning that you can apply the same tag multiple times and an internal counter
will go up. For a tag to be fully removed, it needs to be removed by the same amount of times that it was applied,
although it is possible to force a full removal of a tag.

# Contents

## GameplayTag
- The representation of a tag
- Has a name
- Can have a parent tag
- Has a counter for how many times it was applied

## GameplayTagContainer
- A container for Tags
- Responsible for adding, removing and comparing tags