using Application.DTO;
using MediatR;

namespace Application.Commands;

public sealed record AddItemCommand(int ListId, int ItemId, int Quantity) : IRequest<ListItemDto>;

public sealed record RemoveItemCommand(int ListId, int ListItemId) : IRequest<Unit>;

public record ToggleItemCommand(int ItemId) : IRequest<Unit>;