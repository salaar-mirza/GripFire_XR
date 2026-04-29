# C# Attribute Stacking & API Limits_Notes.md

## The Core Concept: Attribute Constructors
In C#, tags above classes like `[RequireComponent]` or `[SerializeField]` are actually calling **Constructors** of an underlying class.
*   If the original engine developers (like Unity) didn't write a constructor that takes 4 arguments, the compiler will throw an error if you pass 4.

## The Solution: Stacking
If the attribute is marked with `[AttributeUsage(AllowMultiple = true)]`, you can bypass constructor argument limits by stacking the attributes: