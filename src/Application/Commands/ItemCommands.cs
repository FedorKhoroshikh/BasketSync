using Application.DTO;
using MediatR;

namespace Application.Commands;

public sealed record AddListItemCommand(int ListId, int ItemId, int Quantity) : IRequest<ListItemDto>;

public sealed record RemoveItemCommand(int ListId, int ListItemId) : IRequest<Unit>;

public sealed record ToggleItemCommand(int ListId, int ListItemId) : IRequest<Unit>;

public sealed record ToggleListItemByIdCommand(int ListItemId) : IRequest<Unit>;

public sealed record UpdateListItemCommand(int ListItemId, int Quantity) : IRequest<ListItemDto>;