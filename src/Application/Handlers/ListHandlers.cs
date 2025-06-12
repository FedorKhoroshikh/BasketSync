using Application.Commands;
using Application.DTO;
using Application.Exceptions;
using Application.Interfaces;
using Application.Queries;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

#region Handlers for Commands

public sealed class GetAllListsHandler(IUnitOfWork uow, IMapper mapper) : IRequestHandler<GetAllListsQuery, List<ShoppingListDto>>
{
    public async Task<List<ShoppingListDto>> Handle(GetAllListsQuery request, CancellationToken ct)
    {
        var lists = await uow.ShoppingLists
            .GetAll()
            .Where(l => l.User.Id == request.UserId)
            .ProjectTo<ShoppingListDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);
        
        return lists;
    }
}

public sealed class CreateListHandler(IUnitOfWork uow, IMapper mapper) : IRequestHandler<CreateListCommand, ShoppingListDto>
{
    public async Task<ShoppingListDto> Handle(CreateListCommand c, CancellationToken ct)
    {
        var user = await uow.Users.FindAsync(c.UserId, ct)
            ?? throw new KeyNotFoundException($"Пользователь с ID=[{c.UserId}] не найден");

        try
        {
            var list = new ShoppingList(c.Name, user);
            uow.ShoppingLists.Add(list);

            await uow.SaveChangesAsync(ct);
            return mapper.Map<ShoppingListDto>(list);
        }
        catch (DbUpdateException ex) when (ConflictException.IsUniqueViolation(ex))
        {
            throw new ConflictException($"Список с именем '{c.Name}' уже существует");
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Ошибка при сохранении списка", ex);
        }
    }
}

public sealed class RenameListHandler(IUnitOfWork uow, IMapper mapper) : IRequestHandler<RenameListCommand, ShoppingListDto>
{
    public async Task<ShoppingListDto> Handle(RenameListCommand c, CancellationToken ct)
    {
        var list = await uow.ShoppingLists.FindAsync(c.Id, ct)
                   ?? throw new KeyNotFoundException($"Список с ID=[{c.Id}] не найден");
        
        list.Rename(c.Name);
        await uow.SaveChangesAsync(ct);
        
        return mapper.Map<ShoppingListDto>(list);
    }
}

public sealed class RemoveListHandler(IUnitOfWork uow) : IRequestHandler<RemoveListCommand, MediatR.Unit>
{
    public async Task<MediatR.Unit> Handle(RemoveListCommand c, CancellationToken ct)
    {
        var list = await uow.ShoppingLists.FindAsync(c.Id, ct)
                   ?? throw new KeyNotFoundException($"Список с ID=[{c.Id}] не найден");
        
        uow.ShoppingLists.Remove(list);
        await uow.SaveChangesAsync(ct);
        
        return MediatR.Unit.Value;
    }
}

#endregion

#region Handlers for Queries

public sealed class GetListHandler(IUnitOfWork uow, IMapper mapper) : IRequestHandler<GetListQuery, ShoppingListDto>
{
    public async Task<ShoppingListDto> Handle(GetListQuery c, CancellationToken ct)
    {
        var list = await uow.ShoppingLists.FindAsync(c.ListId, ct)
                   ?? throw new KeyNotFoundException($"Список с ID=[{c.ListId}] не найден");
        
        return mapper.Map<ShoppingListDto>(list);
    }
}

#endregion
