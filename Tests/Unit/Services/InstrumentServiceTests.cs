using Domain.Interfaces.Repositories;
using Domain.Services;
using DomainModels.Enums;
using DomainModels.Models;
using Moq;

namespace Tests.Unit.Services;

public class InstrumentServiceTests
{
    private readonly Mock<IInstrumentRepository> _instrumentRepo = new();

    private InstrumentService CreateService()
    {
        return new InstrumentService(_instrumentRepo.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllInstrumentsFromRepository()
    {
        var instruments = new List<Instrument>
        {
            new() { Id = Guid.NewGuid(), Key = InstrumentKey.Guitar6String, DisplayName = "6-String Guitar", StringCount = 6 },
            new() { Id = Guid.NewGuid(), Key = InstrumentKey.Guitar7String, DisplayName = "7-String Guitar", StringCount = 7 }
        };

        _instrumentRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(instruments);

        var result = await CreateService().GetAllAsync();

        Assert.Equal(instruments, result);
        _instrumentRepo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}