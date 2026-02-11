namespace Application.DTO;

public record ShoppingListDto(int Id, int UserId, string Name, List<ListItemDto>? Items = null);

public record ListItemDto(int Id, int ItemId, int Quantity, bool IsChecked,
    string? Comment = null, string? ItemName = null,
    int? CategoryId = null, string? CategoryName = null,
    int? UnitId = null, string? UnitName = null);

public record ItemDto(int Id, string Name, int CategoryId, int UnitId, string? CategoryName = null, string? UnitName = null);

public record UnitDto(int Id, string Name);

public record CategoryDto(int Id, string Name, string? Comment = null);

public record UserDto(int Id, string PwdHash);

public record AuthResultDto(int UserId, string UserName, string Token);

public record DiscountCardDto(int Id, int UserId, string Name, string? Comment, bool IsActive,
    List<CardIdentifierDto>? Identifiers = null);

public record CardIdentifierDto(int Id, int DiscountCardId, int Type, string Value, string? ImagePath = null);