namespace Skindexer.Fetchers.Games.CS2.Fetchers.DTOs;

public record ByMykelRarity(
    string? Id,
    string? Name,
    string? Color
);

public record ByMykelWeapon(
    string? Id,
    string? Name
);

public record ByMykelWear(
    string? Id,
    string? Name
);

public record ByMykelPattern(
    string? Id,
    string? Name
);

public record ByMykelCategory(
    string? Id,
    string? Name
);

public record ByMykelTeam(
    string? Id,
    string? Name
);

public record ByMykelTournament(
    string? Id,
    string? Name
);

public record ByMykelCollection(
    string? Id,
    string? Name,
    string? Image
);

public record ByMykelCrateRef(
    string? Id,
    string? Name,
    string? Image
);
