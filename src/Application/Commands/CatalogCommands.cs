using Application.DTO;
using MediatR;

namespace Application.Commands;

public sealed record CreateCategoryCommand(int Id, string Name) : IRequest<ShoppingListDto>;

public sealed record CreateUnitCommand;