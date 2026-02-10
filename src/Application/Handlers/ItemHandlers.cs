using Application.Commands;
using Application.DTO;
using Application.Interfaces;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public sealed class AddItemHandler(IUnitOfWork uow, IMapper mapper) : IRequestHandler<AddListItemCommand, ListItemDto>
{
    public async Task<ListItemDto> Handle(AddListItemCommand c, CancellationToken ct)
    {
        var list = await uow.ShoppingLists.FindAsync(c.ListId, ct)
            ?? throw new KeyNotFoundException($"Список с ID=[{c.ListId}] не найден");

        var item = await uow.Items.FindAsync(c.ItemId, ct)
            ?? throw new KeyNotFoundException($"Элемент с ID=[{c.ItemId}] не найден");
        
        var listItem = list.AddItem(item, c.Quantity);
        await uow.SaveChangesAsync(ct);
        
        return mapper.Map<ListItemDto>(listItem);
    }
}

public sealed class RemoveItemHandler(IUnitOfWork uow) : IRequestHandler<RemoveItemCommand, Unit>
{
    public async Task<Unit> Handle(RemoveItemCommand c, CancellationToken ct)
    {
        var listItem = await uow.ListItems.FindAsync(c.ListItemId, ct)
                       ?? throw new KeyNotFoundException($"Элемент списка с ID=[{c.ListItemId}] не найден");

        uow.ListItems.Remove(listItem);
        await uow.SaveChangesAsync(ct);

        return Unit.Value;
    }
}

public sealed class ToggleItemHandler(IUnitOfWork uow) : IRequestHandler<ToggleItemCommand, Unit>
{
    public async Task<Unit> Handle(ToggleItemCommand c, CancellationToken ct)
    {
        var list = await uow.ShoppingLists.FindAsync(c.ListId, ct)
                   ?? throw new KeyNotFoundException($"Список с ID=[{c.ListId}] не найден");

        var listItem = list.ListItems.SingleOrDefault(li => li.Id == c.ListItemId)
                   ?? throw new KeyNotFoundException($"Элемент списка с ID=[{c.ListItemId}] не найден");

        listItem.Toggle();

        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

public sealed class ToggleListItemByIdHandler(IUnitOfWork uow) : IRequestHandler<ToggleListItemByIdCommand, Unit>
{
    public async Task<Unit> Handle(ToggleListItemByIdCommand c, CancellationToken ct)
    {
        var listItem = await uow.ListItems.FindAsync(c.ListItemId, ct)
                       ?? throw new KeyNotFoundException($"Элемент списка с ID=[{c.ListItemId}] не найден");

        listItem.Toggle();

        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

public sealed class UpdateListItemHandler(IUnitOfWork uow, IMapper mapper) : IRequestHandler<UpdateListItemCommand, ListItemDto>
{
    public async Task<ListItemDto> Handle(UpdateListItemCommand c, CancellationToken ct)
    {
        var listItem = await uow.ListItems
            .GetAll()
            .Include(li => li.Item).ThenInclude(i => i.Category)
            .Include(li => li.Item).ThenInclude(i => i.Unit)
            .FirstOrDefaultAsync(li => li.Id == c.ListItemId, ct)
            ?? throw new KeyNotFoundException($"Элемент списка с ID=[{c.ListItemId}] не найден");

        if (c.Quantity.HasValue)
            listItem.Quantity = c.Quantity.Value;

        if (c.Comment != null)
            listItem.Comment = c.Comment;

        if (c.CategoryId.HasValue)
        {
            var category = await uow.Categories.FindAsync(c.CategoryId.Value, ct)
                           ?? throw new KeyNotFoundException($"Категория с ID=[{c.CategoryId}] не найдена");
            listItem.Item.SetCategory(category);
        }

        if (c.UnitId.HasValue)
        {
            var unit = await uow.Units.FindAsync(c.UnitId.Value, ct)
                       ?? throw new KeyNotFoundException($"Единица измерения с ID=[{c.UnitId}] не найдена");
            listItem.Item.SetUnit(unit);
        }

        await uow.SaveChangesAsync(ct);
        return mapper.Map<ListItemDto>(listItem);
    }
}