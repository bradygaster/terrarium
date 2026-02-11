namespace Terrarium.Services.Models;

public enum SpeciesServiceStatus
{
    Success,
    AlreadyExists,
    ServerDown,
    VersionIncompatible,
    FiveMinuteThrottle,
    TwentyFourHourThrottle,
    PoliCheckSpeciesNameFailure,
    PoliCheckAuthorNameFailure,
    PoliCheckEmailFailure
}
