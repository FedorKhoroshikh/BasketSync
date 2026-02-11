using Application.DTO;
using MediatR;

namespace Application.Commands;

public sealed record CreateDiscountCardCommand(int UserId, string Name, string? Comment) : IRequest<DiscountCardDto>;

public sealed record UpdateDiscountCardCommand(int Id, string Name, string? Comment) : IRequest<DiscountCardDto>;

public sealed record DeleteDiscountCardCommand(int Id) : IRequest<Unit>;

public sealed record ToggleDiscountCardCommand(int Id) : IRequest<Unit>;

public sealed record AddCardIdentifierCommand(int DiscountCardId, int Type, string Value, string? ImagePath = null) : IRequest<CardIdentifierDto>;

public sealed record RemoveCardIdentifierCommand(int IdentifierId) : IRequest<Unit>;

public sealed record ResolveCardCommand(string Value) : IRequest<DiscountCardDto>;
