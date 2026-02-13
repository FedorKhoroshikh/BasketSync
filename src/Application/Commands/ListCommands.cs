using Application.DTO;
using MediatR;

namespace Application.Commands;

public sealed record CreateListCommand(string Name, int UserId, bool IsShared = true) : IRequest<ShoppingListDto>;

public sealed record RenameListCommand(int Id, string Name, bool IsShared) : IRequest<ShoppingListDto>;

public sealed record RemoveListCommand(int Id) : IRequest<Unit>;

public sealed record UpdateListSharesCommand(int ListId, int RequestingUserId, int[] UserIds) : IRequest<Unit>;