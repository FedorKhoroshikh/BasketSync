using Application.DTO;
using MediatR;

namespace Application.Commands;

public sealed record CreateCategoryCommand(string Name, string? Comment = null) : IRequest<CategoryDto>;

public sealed record UpdateCategoryCommand(int Id, string Name, string? Comment) : IRequest<CategoryDto>;

public sealed record DeleteCategoryCommand(int Id) : IRequest<Unit>;

public sealed record CreateUnitCommand(string Name) : IRequest<UnitDto>;

public sealed record CreateItemCommand(string Name, int CategoryId, int UnitId) : IRequest<ItemDto>;
