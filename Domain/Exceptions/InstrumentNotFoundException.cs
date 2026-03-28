namespace Domain.Exceptions;

/// <summary>
///     Thrown when a referenced InstrumentId does not exist in the system.
///     Maps to HTTP 422 with error code INSTRUMENT_NOT_FOUND.
///     Note: NotFoundException (404) must NOT be used for this case.
/// </summary>
public class InstrumentNotFoundException : BusinessException
{
    public InstrumentNotFoundException(Guid instrumentId)
        : base("INSTRUMENT_NOT_FOUND", $"Instrument '{instrumentId}' was not found.")
    {
        StatusCode = 422;
    }
}