using Api.Mapping;
using ApiModels.Chords;
using ApiModels.Instruments;
using ApiModels.Users;
using AutoMapper;
using DomainModels.Enums;
using DomainModels.Models;
using Repository.Mapping;

namespace Tests.Unit.Mapping;

public class DomainToResponseProfileTests
{
    private static IMapper CreateMapper()
    {
        return new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<EntityToDomainProfile>();
            cfg.AddProfile<DomainToResponseProfile>();
        }).CreateMapper();
    }

    [Fact]
    public void UserToUserResponse_EnglishLanguage_MapsToEn()
    {
        var mapper = CreateMapper();
        var user = new User
        {
            Id = Guid.NewGuid(), Email = "a@a.com",
            FirstName = "F", LastName = "L",
            Language = Language.English
        };

        var response = mapper.Map<UserResponse>(user);

        Assert.Equal("en", response.Language);
    }

    [Fact]
    public void UserToUserResponse_HungarianLanguage_MapsToHu()
    {
        var mapper = CreateMapper();
        var user = new User
        {
            Id = Guid.NewGuid(), Email = "a@a.com",
            FirstName = "F", LastName = "L",
            Language = Language.Hungarian
        };

        var response = mapper.Map<UserResponse>(user);

        Assert.Equal("hu", response.Language);
    }

    [Fact]
    public void UserSavedPresetToPresetResponse_DeserializesStyles()
    {
        var mapper = CreateMapper();
        var stylesJson = """[{"ModuleType":"Title","StylesJson":"{}"}]""";
        var preset = new UserSavedPreset { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Test", StylesJson = stylesJson };

        var response = mapper.Map<PresetResponse>(preset);

        Assert.Equal("Test", response.Name);
        Assert.Single(response.Styles);
        Assert.Equal("Title", response.Styles[0].ModuleType);
    }

    // ── Instrument → InstrumentResponse ──────────────────────────────────

    [Fact]
    public void InstrumentToInstrumentResponse_MapsKeyAsStringAndDisplayNameAsName()
    {
        var mapper = CreateMapper();
        var instrument = new Instrument
        {
            Id = Guid.NewGuid(),
            Key = InstrumentKey.Guitar6String,
            DisplayName = "6-String Guitar",
            StringCount = 6
        };

        var response = mapper.Map<InstrumentResponse>(instrument);

        Assert.Equal(instrument.Id, response.Id);
        Assert.Equal("Guitar6String", response.Key);
        Assert.Equal("6-String Guitar", response.Name);
        Assert.Equal(6, response.StringCount);
    }

    // ── ChordBarre → ChordBarreResponse ──────────────────────────────────

    [Fact]
    public void ChordBarreToChordBarreResponse_MapsAllFields()
    {
        var mapper = CreateMapper();
        var barre = new ChordBarre { Fret = 5, FromString = 1, StringTo = 6 };

        var response = mapper.Map<ChordBarreResponse>(barre);

        Assert.Equal(5, response.Fret);
        Assert.Equal(1, response.FromString);
        Assert.Equal(6, response.StringTo);
    }

    // ── ChordString → ChordStringResponse ────────────────────────────────

    [Theory]
    [InlineData(ChordStringState.Fretted, "fretted")]
    [InlineData(ChordStringState.Open, "open")]
    [InlineData(ChordStringState.Muted, "muted")]
    public void ChordStringToChordStringResponse_MapsStringNumberAndLowercaseState(
        ChordStringState state, string expectedState)
    {
        var mapper = CreateMapper();
        var str = new ChordString { StringNumber = 3, State = state, Fret = 2, Finger = 1 };

        var response = mapper.Map<ChordStringResponse>(str);

        Assert.Equal(3, response.String);
        Assert.Equal(expectedState, response.State);
        Assert.Equal(2, response.Fret);
        Assert.Equal(1, response.Finger);
    }

    // ── ChordPosition → ChordPositionResponse ────────────────────────────

    [Fact]
    public void ChordPositionToChordPositionResponse_MapsBarreAndStrings()
    {
        var mapper = CreateMapper();
        var position = new ChordPosition
        {
            Label = "1",
            BaseFret = 1,
            Barre = new ChordBarre { Fret = 1, FromString = 1, StringTo = 6 },
            Strings = [new ChordString { StringNumber = 6, State = ChordStringState.Fretted, Fret = 1, Finger = 1 }]
        };

        var response = mapper.Map<ChordPositionResponse>(position);

        Assert.Equal("1", response.Label);
        Assert.Equal(1, response.BaseFret);
        Assert.NotNull(response.Barre);
        Assert.Equal(6, response.Barre!.StringTo);
        Assert.Single(response.Strings);
        Assert.Equal(6, response.Strings[0].String);
    }

    [Fact]
    public void ChordPositionToChordPositionResponse_NullBarre_MapsToNullBarre()
    {
        var mapper = CreateMapper();
        var position = new ChordPosition { Label = "1", BaseFret = 1, Barre = null, Strings = [] };

        var response = mapper.Map<ChordPositionResponse>(position);

        Assert.Null(response.Barre);
    }

    // ── Chord → ChordSummaryResponse ─────────────────────────────────────

    [Fact]
    public void ChordToChordSummaryResponse_MapsPreviewPositionFromFirstPosition()
    {
        var mapper = CreateMapper();
        var chord = MakeChord();

        var response = mapper.Map<ChordSummaryResponse>(chord);

        Assert.Equal(chord.Id, response.Id);
        Assert.Equal("Guitar6String", response.InstrumentKey);
        Assert.Equal("Am", response.Name);
        Assert.Equal("A", response.Root);
        Assert.Equal("Minor", response.Quality);
        Assert.Null(response.Extension);
        Assert.Null(response.Alternation);
        Assert.NotNull(response.PreviewPosition);
        Assert.Equal("1", response.PreviewPosition.Label);
        Assert.False(response is ChordDetailResponse);
    }

    // ── Chord → ChordDetailResponse ───────────────────────────────────────

    [Fact]
    public void ChordToChordDetailResponse_MapsAllPositions()
    {
        var mapper = CreateMapper();
        var chord = MakeChord();

        var response = mapper.Map<ChordDetailResponse>(chord);

        Assert.Equal(chord.Id, response.Id);
        Assert.Equal("Guitar6String", response.InstrumentKey);
        Assert.Equal(2, response.Positions.Count);
    }

    [Fact]
    public void ChordToChordSummaryResponse_NullableExtensionAndAlternationPassThrough()
    {
        var mapper = CreateMapper();
        var chord = MakeChord();
        chord.Extension = "add9";
        chord.Alternation = "#9";

        var response = mapper.Map<ChordSummaryResponse>(chord);

        Assert.Equal("add9", response.Extension);
        Assert.Equal("#9", response.Alternation);
    }

    // ── fixtures ─────────────────────────────────────────────────────────

    private static Chord MakeChord() => new()
    {
        Id = Guid.NewGuid(),
        InstrumentId = Guid.NewGuid(),
        InstrumentKey = InstrumentKey.Guitar6String,
        Name = "Am",
        Root = "A",
        Quality = "Minor",
        Extension = null,
        Alternation = null,
        Positions =
        [
            new ChordPosition
            {
                Label = "1",
                BaseFret = 1,
                Barre = null,
                Strings = [new ChordString { StringNumber = 6, State = ChordStringState.Open }]
            },
            new ChordPosition
            {
                Label = "2",
                BaseFret = 5,
                Barre = new ChordBarre { Fret = 5, FromString = 1, StringTo = 6 },
                Strings = [new ChordString { StringNumber = 6, State = ChordStringState.Fretted, Fret = 5, Finger = 1 }]
            }
        ]
    };
}