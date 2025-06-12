using Application.DTO;
using MediatR;

namespace Application.Commands;

public sealed record CreateListCommand(string Name, int UserId) : IRequest<ShoppingListDto>;

public sealed record RenameListCommand(int Id, string Name) : IRequest<ShoppingListDto>, IRequest<string>;

public sealed record RemoveListCommand(int Id) : IRequest<Unit>;