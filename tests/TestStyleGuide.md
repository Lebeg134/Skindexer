# NumeromancerNeo — Test Style Guide

## Stack

| Item | Detail |
|------|--------|
| Framework | .NET 9 xUnit |
| Mocking | NSubstitute |
| Assertions | Plain `xUnit Assert.*` — no FluentAssertions |

---

## Naming Convention

Pattern: `MethodOrConcept_StateUnderTest_ExpectedBehavior`

```
Constructor_WithValidData_CreatesSuccessfully
Constructor_WithEmptyItems_CreatesSuccessfully
Generate_WithNullSource_ThrowsArgumentNullException
Generate_WithPruningPredicateThatPrunesAll_ProducesNoCombinations
```

---

## File Structure

Tests are grouped using `#region` blocks by concern, in this order:

1. `Test Data Builders` — helpers and factory methods
2. `Construction` — constructor tests (if applicable)
3. `Guard Clauses` — argument validation / exception tests
4. Feature-specific regions — e.g. `GetItem`, `Correctness`, `Pruning`, `Span Safety`

```csharp
public class ExampleTests
{
    #region Test Data Builders
    // private static helpers here
    #endregion

    #region Guard Clauses
    // ArgumentNullException, ArgumentOutOfRangeException, etc.
    #endregion

    #region Correctness
    // core behaviour tests
    #endregion
}
```

---

## Test Data

Named variables declared explicitly before use — never inline at the call site.

```csharp
// correct
var source = new List<int> { 1, 2, 3 };
var results = Collect(source, 2);

// avoid
var results = Collect([1, 2, 3], 2);
```

Collection literals:
- Source data → `new List<T> { }`
- Empty collections → `[]`

---

## Helpers

Reusable logic lives in `#region Test Data Builders` as `private static` methods.

- **Data factories** return tuples: `BuildValidTestData()` → `(Items, Collections, Grades)`
- **Wrappers** encapsulate the system under test: `Collect()` wraps `CombinationGenerator<T>.Generate`

```csharp
private static List<int[]> Collect(
    IReadOnlyList<int> source,
    int n,
    PruningPredicate<int>? shouldPrune = null)
{
    var results = new List<int[]>();
    CombinationGenerator<int>.Generate(
        source, n,
        span => results.Add(span.ToArray()),
        shouldPrune);
    return results;
}
```

---

## Variable Naming Inside Tests

Follow the `source` → `results` → assertions pattern.

```csharp
var source = new List<int> { 1, 2, 3 };  // input
var results = Collect(source, 2);          // output

var distinctCount = results               // intermediate — named meaningfully
    .Select(c => string.Join(",", c))
    .Distinct()
    .Count();

Assert.Equal(results.Count, distinctCount);
```

Mocked dependencies are named after their type: `gameData`, `solver`, `calculator`.

---

## Comments

Used sparingly — only when a scenario requires a non-obvious formula or expectation clarified.

```csharp
// Pool [1,2,3], n=2 with repetition:
// [1,1],[1,2],[1,3],[2,2],[2,3],[3,3] = 6 combinations
var results = Collect(source, 2);
Assert.Equal(6, results.Count);
```Use new List<T> { } for source data. Empty collections use [].