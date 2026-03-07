# Quickstart: Domain Model Implementation

**Branch**: `002-domain-models` | **Date**: 2026-03-02

---

## Prerequisites

- .NET 10 SDK installed (`dotnet --version` should report `10.x.x`)
- Solution builds clean from the `001-solution-scaffold` baseline

---

## 1. Build Only the DomainModels Project

```bash
cd /path/to/Staccato/Backend
dotnet build DomainModels/DomainModels.csproj
```

Expected: zero errors, zero warnings. DomainModels has no project references —
it must compile entirely on its own.

---

## 2. Build the Full Solution

```bash
dotnet build Staccato.sln
```

All 9 projects must compile. DomainModels being error-free is the gate for
Domain, Repository, Api, and Tests (which all reference it).

---

## 3. Verify Zero Project References

```bash
grep -i "ProjectReference" DomainModels/DomainModels.csproj
```

Expected: no output. The `.csproj` must contain no `<ProjectReference>` elements.

---

## 4. Using Enums

```csharp
using DomainModels.Enums;

var size = PageSize.A4;
var type = ModuleType.Theory;
var status = ExportStatus.Pending;
```

---

## 5. Using Domain Models

```csharp
using DomainModels.Models;
using DomainModels.Enums;

var notebook = new Notebook
{
    Id = Guid.NewGuid(),
    UserId = Guid.NewGuid(),
    Title = "Guitar Fundamentals",
    InstrumentId = Guid.NewGuid(),
    PageSize = PageSize.A4,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};
```

---

## 6. Using Building Blocks

```csharp
using DomainModels.BuildingBlocks;
using DomainModels.Enums;

// Plain text with a bold word
var block = new TextBlock
{
    Spans =
    [
        new TextSpan { Text = "Practice ", Bold = false },
        new TextSpan { Text = "slowly", Bold = true }
    ]
};

// Checkbox list with completion state
var checklist = new CheckboxListBlock
{
    Items =
    [
        new CheckboxListItem
        {
            Spans = [new TextSpan { Text = "Warm up 10 min", Bold = false }],
            IsChecked = true
        },
        new CheckboxListItem
        {
            Spans = [new TextSpan { Text = "Practice scales", Bold = false }],
            IsChecked = false
        }
    ]
};

// Chord progression
var progression = new ChordProgressionBlock
{
    TimeSignature = "4/4",
    Sections =
    [
        new ChordProgressionSection
        {
            Label = "Verse",
            Repeat = 2,
            Measures =
            [
                new ChordMeasure
                {
                    Chords =
                    [
                        new ChordBeat { ChordId = Guid.NewGuid(), DisplayName = "Am", Beats = 4 }
                    ]
                }
            ]
        }
    ]
};
```

---

## 7. Using ModuleTypeConstraints

```csharp
using DomainModels.Constants;
using DomainModels.Enums;

// Check allowed blocks for a module type
var allowed = ModuleTypeConstraints.AllowedBlocks[ModuleType.Theory];
bool canUseChordProgression = allowed.Contains(BuildingBlockType.ChordProgression); // true

// Check minimum dimensions
var (minW, minH) = ModuleTypeConstraints.MinimumSizes[ModuleType.ChordTablature];
// minW = 8, minH = 10

// Breadcrumb always has empty allowed set
bool breadcrumbHasNoBlocks = ModuleTypeConstraints.AllowedBlocks[ModuleType.Breadcrumb].Count == 0; // true
```

---

## 8. Using PageSizeDimensions

```csharp
using DomainModels.Constants;
using DomainModels.Enums;

var (widthMm, heightMm, gridW, gridH) = PageSizeDimensions.Dimensions[PageSize.A4];
// widthMm=210, heightMm=297, gridW=42, gridH=59

// Validate a module fits on the page
bool fitsHorizontally = module.GridX + module.GridWidth <= gridW;
bool fitsVertically   = module.GridY + module.GridHeight <= gridH;
```

---

## 9. Run Tests Against DomainModels

```bash
# Unit tests covering ModuleTypeConstraints and PageSizeDimensions completeness
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Unit.DomainModels"
```

Expected: all tests pass. The unit test suite verifies:
- Every ModuleType has an entry in both dictionaries
- Every PageSize has an entry with correct grid dimensions
- Breadcrumb maps to an empty allowed-block set
- ChordTablature maps to a set containing only ChordTablatureGroup
- All 10 building block types instantiate with a correct Type discriminator
