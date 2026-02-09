using Application.DTO;
using MediatR;

namespace Application.Commands;

public sealed record CreateCategoryCommand(string Name) : IRequest<CategoryDto>;

public sealed record CreateUnitCommand(string Name) : IRequest<UnitDto>;

public sealed record CreateItemCommand(string Name, int CategoryId, int UnitId) : IRequest<ItemDto>;
