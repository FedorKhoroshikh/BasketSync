namespace Application.DTO;

public record ShoppingListDto(int Id, int UserId, string Name);

public record ListItemDto(int Id, int ItemId, int Quantity, bool IsChecked, string ItemName);

public record ItemDto();

public record UnitDto(int Id, string Name);

public record CategoryDto(int Id, string Name);

public record UserDto(int Id, string PwdHash);