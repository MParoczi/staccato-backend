# Type Hierarchy Contract: DomainModels

**Branch**: `002-domain-models` | **Date**: 2026-03-02

DomainModels is a zero-dependency class library. It has no HTTP endpoints,
message bus contracts, or CLI interfaces. Its "contracts" are the public types
it exposes to the Domain, Repository, Api, and Tests projects.

---

## Namespace Index

```
DomainModels.Enums
├── ModuleType          (12 values)
├── BuildingBlockType   (10 values)
├── BorderStyle         (4 values)
├── FontFamily          (3 values)
├── PageSize            (5 values)
├── ExportStatus        (4 values)
├── InstrumentKey       (7 values)
├── ChordStringState    (3 values)
└── Language            (2 values)

DomainModels.Models
├── User
├── RefreshToken
├── UserSavedPreset
├── SystemStylePreset
├── Instrument
├── Chord
├── Notebook
├── NotebookModuleStyle
├── Lesson
├── LessonPage
├── Module
└── PdfExport

DomainModels.BuildingBlocks
├── TextSpan                     (leaf value type)
├── BuildingBlock                (abstract base)
├── SectionHeadingBlock          : BuildingBlock
├── DateBlock                    : BuildingBlock
├── TextBlock                    : BuildingBlock
├── BulletListBlock              : BuildingBlock
├── NumberedListBlock            : BuildingBlock
├── CheckboxListItem             (support type)
├── CheckboxListBlock            : BuildingBlock
├── TableColumn                  (support type)
├── TableBlock                   : BuildingBlock
├── MusicalNotesBlock            : BuildingBlock
├── ChordBeat                    (support type)
├── ChordMeasure                 (support type)
├── ChordProgressionSection      (support type)
├── ChordProgressionBlock        : BuildingBlock
├── ChordTablatureItem           (support type)
└── ChordTablatureGroupBlock     : BuildingBlock

DomainModels.Constants
├── ModuleTypeConstraints        (static class)
│   ├── AllowedBlocks            IReadOnlyDictionary<ModuleType, IReadOnlySet<BuildingBlockType>>
│   └── MinimumSizes             IReadOnlyDictionary<ModuleType, (int MinWidth, int MinHeight)>
└── PageSizeDimensions           (static class)
    └── Dimensions               IReadOnlyDictionary<PageSize, (int WidthMm, int HeightMm, int GridWidth, int GridHeight)>
```

---

## Consumer Map

| Consuming Project | What It Uses |
|---|---|
| `Domain` | Models (service parameters/return types), BuildingBlocks (content validation), Constants (ModuleTypeConstraints for overlap/size checks), Enums |
| `Repository` | Models (entity↔domain mapping via AutoMapper), Enums |
| `Api` | Models (service return types → response DTOs), Enums (request DTO fields) |
| `ApiModels` | Enums (request DTO fields like `PageSize`, `ModuleType`) |
| `EntityModels` | Enums (column types in entity classes) |
| `Tests` | All namespaces (unit + integration testing) |

---

## Invariants This Library Guarantees

1. Every enum value is reachable with no external dependency.
2. `BuildingBlock.Type` is always set at construction — no `default` enum value.
3. `ModuleTypeConstraints.AllowedBlocks` and `MinimumSizes` both contain entries
   for all 12 `ModuleType` values.
4. `PageSizeDimensions.Dimensions` contains entries for all 5 `PageSize` values.
5. `TextSpan` has exactly two properties — no formatting beyond text + bold.
6. No type in this library carries EF Core, FluentValidation, or JSON serialization
   framework attributes.
