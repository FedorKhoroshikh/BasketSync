namespace Application.DTO;

public record ShoppingListDto(int Id, int UserId, string Name, List<ListItemDto>? Items = null);

public record ListItemDto(int Id, int ItemId, int Quantity, bool IsChecked, string? ItemName = null, string? CategoryName = null, string? UnitName = null);

public record ItemDto(int Id, string Name, int CategoryId, int UnitId, string? CategoryName = null, string? UnitName = null);

public record UnitDto(int Id, string Name);

public record CategoryDto(int Id, string Name);

public record UserDto(int Id, string PwdHash);