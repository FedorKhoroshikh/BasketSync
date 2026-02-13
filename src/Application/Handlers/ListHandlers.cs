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
            .Where(l => l.User.Id == request.UserId
                        || l.IsShared
                        || l.Shares.Any(s => s.UserId == request.UserId))
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
            list.SetShared(c.IsShared);
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
        list.SetShared(c.IsShared);
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
        var list = await uow.ShoppingLists
                       .GetAll()
                       .Include(l => l.ListItems)
                           .ThenInclude(li => li.Item)
                               .ThenInclude(i => i.Category)
                       .Include(l => l.ListItems)
                           .ThenInclude(li => li.Item)
                               .ThenInclude(i => i.Unit)
                       .FirstOrDefaultAsync(l => l.Id == c.ListId, ct)
                   ?? throw new KeyNotFoundException($"Список с ID=[{c.ListId}] не найден");

        return mapper.Map<ShoppingListDto>(list);
    }
}

public sealed class GetAllUsersHandler(IUnitOfWork uow) : IRequestHandler<GetAllUsersQuery, List<UserSummaryDto>>
{
    public async Task<List<UserSummaryDto>> Handle(GetAllUsersQuery request, CancellationToken ct)
    {
        return await uow.Users.GetAll()
            .OrderBy(u => u.Name)
            .Select(u => new UserSummaryDto(u.Id, u.Name))
            .ToListAsync(ct);
    }
}

public sealed class GetListSharesHandler(IUnitOfWork uow) : IRequestHandler<GetListSharesQuery, int[]>
{
    public async Task<int[]> Handle(GetListSharesQuery request, CancellationToken ct)
    {
        return await uow.ListShares.GetAll()
            .Where(ls => ls.ShoppingListId == request.ListId)
            .Select(ls => ls.UserId)
            .ToArrayAsync(ct);
    }
}

public sealed class UpdateListSharesHandler(IUnitOfWork uow) : IRequestHandler<UpdateListSharesCommand, MediatR.Unit>
{
    public async Task<MediatR.Unit> Handle(UpdateListSharesCommand c, CancellationToken ct)
    {
        var list = await uow.ShoppingLists.GetAll()
            .Include(l => l.Shares)
            .FirstOrDefaultAsync(l => l.Id == c.ListId, ct)
            ?? throw new KeyNotFoundException($"Список с ID=[{c.ListId}] не найден");

        if (list.UserId != c.RequestingUserId)
            throw new UnauthorizedAccessException("Только владелец может менять доступ");

        // Remove existing shares
        var existing = list.Shares.ToList();
        foreach (var share in existing)
            uow.ListShares.Remove(share);

        // Add new shares (exclude owner)
        foreach (var userId in c.UserIds.Where(id => id != list.UserId).Distinct())
        {
            var user = await uow.Users.FindAsync(userId, ct);
            if (user is not null)
                uow.ListShares.Add(new ListShare(list, user));
        }

        await uow.SaveChangesAsync(ct);
        return MediatR.Unit.Value;
    }
}

#endregion
