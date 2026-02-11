using Application.Commands;
using Application.DTO;
using Application.Exceptions;
using Application.Interfaces;
using Application.Queries;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public sealed class GetUserCardsHandler(IUnitOfWork uow, IMapper mapper)
    : IRequestHandler<GetUserCardsQuery, List<DiscountCardDto>>
{
    public async Task<List<DiscountCardDto>> Handle(GetUserCardsQuery request, CancellationToken ct)
    {
        var cards = await uow.DiscountCards
            .GetAll()
            .Where(c => c.UserId == request.UserId)
            .Include(c => c.Identifiers)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        return mapper.Map<List<DiscountCardDto>>(cards);
    }
}

public sealed class GetCardByIdHandler(IUnitOfWork uow, IMapper mapper)
    : IRequestHandler<GetCardByIdQuery, DiscountCardDto>
{
    public async Task<DiscountCardDto> Handle(GetCardByIdQuery request, CancellationToken ct)
    {
        var card = await uow.DiscountCards
            .GetAll()
            .Include(c => c.Identifiers)
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Карта с ID=[{request.Id}] не найдена");

        return mapper.Map<DiscountCardDto>(card);
    }
}

public sealed class CreateDiscountCardHandler(IUnitOfWork uow, IMapper mapper)
    : IRequestHandler<CreateDiscountCardCommand, DiscountCardDto>
{
    public async Task<DiscountCardDto> Handle(CreateDiscountCardCommand c, CancellationToken ct)
    {
        var user = await uow.Users.FindAsync(c.UserId, ct)
                   ?? throw new KeyNotFoundException($"Пользователь с ID=[{c.UserId}] не найден");

        var card = new DiscountCard(user, c.Name, c.Comment);
        uow.DiscountCards.Add(card);
        await uow.SaveChangesAsync(ct);
        return mapper.Map<DiscountCardDto>(card);
    }
}

public sealed class UpdateDiscountCardHandler(IUnitOfWork uow, IMapper mapper)
    : IRequestHandler<UpdateDiscountCardCommand, DiscountCardDto>
{
    public async Task<DiscountCardDto> Handle(UpdateDiscountCardCommand c, CancellationToken ct)
    {
        var card = await uow.DiscountCards.FindAsync(c.Id, ct)
                   ?? throw new KeyNotFoundException($"Карта с ID=[{c.Id}] не найдена");

        card.Update(c.Name, c.Comment);
        await uow.SaveChangesAsync(ct);
        return mapper.Map<DiscountCardDto>(card);
    }
}

public sealed class DeleteDiscountCardHandler(IUnitOfWork uow, IFileStorageService fileStorage)
    : IRequestHandler<DeleteDiscountCardCommand, MediatR.Unit>
{
    public async Task<MediatR.Unit> Handle(DeleteDiscountCardCommand c, CancellationToken ct)
    {
        var card = await uow.DiscountCards
            .GetAll()
            .Include(d => d.Identifiers)
            .FirstOrDefaultAsync(d => d.Id == c.Id, ct)
            ?? throw new KeyNotFoundException($"Карта с ID=[{c.Id}] не найдена");

        foreach (var identifier in card.Identifiers)
        {
            if (!string.IsNullOrEmpty(identifier.ImagePath))
                fileStorage.Delete(identifier.ImagePath);
        }

        uow.DiscountCards.Remove(card);
        await uow.SaveChangesAsync(ct);
        return MediatR.Unit.Value;
    }
}

public sealed class ToggleDiscountCardHandler(IUnitOfWork uow)
    : IRequestHandler<ToggleDiscountCardCommand, MediatR.Unit>
{
    public async Task<MediatR.Unit> Handle(ToggleDiscountCardCommand c, CancellationToken ct)
    {
        var card = await uow.DiscountCards.FindAsync(c.Id, ct)
                   ?? throw new KeyNotFoundException($"Карта с ID=[{c.Id}] не найдена");

        if (card.IsActive) card.Deactivate();
        else card.Activate();

        await uow.SaveChangesAsync(ct);
        return MediatR.Unit.Value;
    }
}

public sealed class AddCardIdentifierHandler(IUnitOfWork uow, IMapper mapper)
    : IRequestHandler<AddCardIdentifierCommand, CardIdentifierDto>
{
    public async Task<CardIdentifierDto> Handle(AddCardIdentifierCommand c, CancellationToken ct)
    {
        var card = await uow.DiscountCards
            .GetAll()
            .Include(d => d.Identifiers)
            .FirstOrDefaultAsync(d => d.Id == c.DiscountCardId, ct)
            ?? throw new KeyNotFoundException($"Карта с ID=[{c.DiscountCardId}] не найдена");

        var identifier = card.AddIdentifier((IdentifierType)c.Type, c.Value, c.ImagePath);
        await uow.SaveChangesAsync(ct);
        return mapper.Map<CardIdentifierDto>(identifier);
    }
}

public sealed class RemoveCardIdentifierHandler(IUnitOfWork uow, IFileStorageService fileStorage)
    : IRequestHandler<RemoveCardIdentifierCommand, MediatR.Unit>
{
    public async Task<MediatR.Unit> Handle(RemoveCardIdentifierCommand c, CancellationToken ct)
    {
        var identifier = await uow.CardIdentifiers.FindAsync(c.IdentifierId, ct)
                         ?? throw new KeyNotFoundException($"Идентификатор с ID=[{c.IdentifierId}] не найден");

        if (!string.IsNullOrEmpty(identifier.ImagePath))
            fileStorage.Delete(identifier.ImagePath);

        uow.CardIdentifiers.Remove(identifier);
        await uow.SaveChangesAsync(ct);
        return MediatR.Unit.Value;
    }
}

public sealed class ResolveCardHandler(IUnitOfWork uow, IMapper mapper)
    : IRequestHandler<ResolveCardCommand, DiscountCardDto>
{
    public async Task<DiscountCardDto> Handle(ResolveCardCommand c, CancellationToken ct)
    {
        var identifier = await uow.CardIdentifiers
            .GetAll()
            .Include(ci => ci.DiscountCard)
                .ThenInclude(d => d.Identifiers)
            .FirstOrDefaultAsync(ci => ci.Value == c.Value, ct)
            ?? throw new KeyNotFoundException($"Карта с идентификатором '{c.Value}' не найдена");

        if (!identifier.DiscountCard.IsActive)
            throw new KeyNotFoundException("Карта деактивирована");

        return mapper.Map<DiscountCardDto>(identifier.DiscountCard);
    }
}
