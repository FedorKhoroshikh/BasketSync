using Application.DTO;
using MediatR;

namespace Application.Queries;

public sealed record GetUserCardsQuery(int UserId) : IRequest<List<DiscountCardDto>>;

public sealed record GetCardByIdQuery(int Id) : IRequest<DiscountCardDto>;
