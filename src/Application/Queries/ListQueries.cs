using Application.DTO;
using MediatR;

namespace Application.Queries;

public sealed record GetListQuery(int ListId) : IRequest<ShoppingListDto>;

public sealed record GetAllListsQuery(int UserId) : IRequest<List<ShoppingListDto>>;