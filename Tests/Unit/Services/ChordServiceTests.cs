using Domain.Exceptions;
using Domain.Interfaces.Repositories;
using Domain.Services;
using DomainModels.Enums;
using DomainModels.Models;
using Moq;

namespace Tests.Unit.Services;

public class ChordServiceTests
{
    private readonly Mock<IChordRepository> _chordRepo = new();
    private readonly Mock<IInstrumentRepository> _instrumentRepo = new();

    private ChordService CreateService()
    {
        return new ChordService(_chordRepo.Object, _instrumentRepo.Object);
    }

    private static Instrument MakeInstrument(InstrumentKey key = InstrumentKey.Guitar6String)
    {
        return new Instrument
        {
            Id = Guid.NewGuid(),
            Key = key,
            DisplayName = "6-String Guitar",
            StringCount = 6
        };
    }

    private static Chord MakeChord(Guid? instrumentId = null)
    {
        return new Chord
        {
            Id = Guid.NewGuid(),
            InstrumentId = instrumentId ?? Guid.NewGuid(),
            InstrumentKey = InstrumentKey.Guitar6String,
            Name = "Am",
            Root = "A",
            Quality = "Minor",
            Positions =
            [
                new ChordPosition
                {
                    Label = "1",
                    BaseFret = 1,
                    Barre = null,
                    Strings = []
                }
            ]
        };
    }

    // ── SearchAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_ValidInstrument_ReturnsChordList()
    {
        var instrument = MakeInstrument();
        var chords = new List<Chord> { MakeChord(instrument.Id) };

        _instrumentRepo
            .Setup(r => r.GetByKeyAsync(InstrumentKey.Guitar6String, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);
        _chordRepo
            .Setup(r => r.SearchAsync(instrument.Id, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chords);

        var result = await CreateService().SearchAsync(InstrumentKey.Guitar6String, null, null);

        Assert.Equal(chords, result);
    }

    [Fact]
    public async Task SearchAsync_WithRootAndQualityFilters_PassesFiltersToRepository()
    {
        var instrument = MakeInstrument();
        var chords = new List<Chord> { MakeChord(instrument.Id) };

        _instrumentRepo
            .Setup(r => r.GetByKeyAsync(InstrumentKey.Guitar6String, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);
        _chordRepo
            .Setup(r => r.SearchAsync(instrument.Id, "A", "Minor", It.IsAny<CancellationToken>()))
            .ReturnsAsync(chords);

        var result = await CreateService().SearchAsync(InstrumentKey.Guitar6String, "A", "Minor");

        Assert.Equal(chords, result);
        _chordRepo.Verify(r => r.SearchAsync(instrument.Id, "A", "Minor", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_InstrumentNotInDatabase_ThrowsNotFoundException()
    {
        _instrumentRepo
            .Setup(r => r.GetByKeyAsync(InstrumentKey.Guitar6String, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Instrument?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => CreateService().SearchAsync(InstrumentKey.Guitar6String, null, null));

        Assert.Equal("INSTRUMENT_NOT_FOUND", ex.Code);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingChord_ReturnsChord()
    {
        var chord = MakeChord();
        _chordRepo
            .Setup(r => r.GetByIdAsync(chord.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chord);

        var result = await CreateService().GetByIdAsync(chord.Id);

        Assert.Equal(chord, result);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _chordRepo
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Chord?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().GetByIdAsync(id));
    }
}