using DomainModels.Enums;

namespace DomainModels.Constants;

public static class ModuleTypeConstraints
{
    public static readonly IReadOnlyDictionary<ModuleType, IReadOnlySet<BuildingBlockType>> AllowedBlocks =
        new Dictionary<ModuleType, IReadOnlySet<BuildingBlockType>>
        {
            {
                ModuleType.Title,
                new HashSet<BuildingBlockType> { BuildingBlockType.Date, BuildingBlockType.Text }
            },
            {
                ModuleType.Breadcrumb,
                new HashSet<BuildingBlockType>()
            },
            {
                ModuleType.Subtitle,
                new HashSet<BuildingBlockType> { BuildingBlockType.Text }
            },
            {
                ModuleType.Theory,
                new HashSet<BuildingBlockType>
                {
                    BuildingBlockType.SectionHeading,
                    BuildingBlockType.Text,
                    BuildingBlockType.BulletList,
                    BuildingBlockType.NumberedList,
                    BuildingBlockType.Table,
                    BuildingBlockType.MusicalNotes
                }
            },
            {
                ModuleType.Practice,
                new HashSet<BuildingBlockType>
                {
                    BuildingBlockType.SectionHeading,
                    BuildingBlockType.Text,
                    BuildingBlockType.ChordProgression,
                    BuildingBlockType.ChordTablatureGroup,
                    BuildingBlockType.MusicalNotes
                }
            },
            {
                ModuleType.Example,
                new HashSet<BuildingBlockType>
                {
                    BuildingBlockType.SectionHeading,
                    BuildingBlockType.Text,
                    BuildingBlockType.ChordProgression,
                    BuildingBlockType.MusicalNotes
                }
            },
            {
                ModuleType.Important,
                new HashSet<BuildingBlockType>
                {
                    BuildingBlockType.SectionHeading,
                    BuildingBlockType.Text,
                    BuildingBlockType.MusicalNotes
                }
            },
            {
                ModuleType.Tip,
                new HashSet<BuildingBlockType>
                {
                    BuildingBlockType.SectionHeading,
                    BuildingBlockType.Text,
                    BuildingBlockType.MusicalNotes
                }
            },
            {
                ModuleType.Homework,
                new HashSet<BuildingBlockType>
                {
                    BuildingBlockType.SectionHeading,
                    BuildingBlockType.Text,
                    BuildingBlockType.BulletList,
                    BuildingBlockType.NumberedList,
                    BuildingBlockType.CheckboxList
                }
            },
            {
                ModuleType.Question,
                new HashSet<BuildingBlockType>
                {
                    BuildingBlockType.SectionHeading,
                    BuildingBlockType.Text
                }
            },
            {
                ModuleType.ChordTablature,
                new HashSet<BuildingBlockType>
                {
                    BuildingBlockType.ChordTablatureGroup,
                    BuildingBlockType.MusicalNotes
                }
            },
            {
                ModuleType.FreeText,
                new HashSet<BuildingBlockType>(Enum.GetValues<BuildingBlockType>())
            }
        };

    public static readonly IReadOnlyDictionary<ModuleType, (int MinWidth, int MinHeight)> MinimumSizes =
        new Dictionary<ModuleType, (int MinWidth, int MinHeight)>
        {
            { ModuleType.Title, (20, 4) },
            { ModuleType.Breadcrumb, (20, 3) },
            { ModuleType.Subtitle, (10, 3) },
            { ModuleType.Theory, (8, 5) },
            { ModuleType.Practice, (8, 5) },
            { ModuleType.Example, (8, 5) },
            { ModuleType.Important, (8, 4) },
            { ModuleType.Tip, (8, 4) },
            { ModuleType.Homework, (8, 5) },
            { ModuleType.Question, (8, 4) },
            { ModuleType.ChordTablature, (8, 10) },
            { ModuleType.FreeText, (4, 4) }
        };
}