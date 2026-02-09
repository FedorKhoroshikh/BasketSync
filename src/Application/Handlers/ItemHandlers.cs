using Application.Commands;
using Application.DTO;
using Application.Interfaces;
using AutoMapper;
using MediatR;

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
        var list = await uow.ShoppingLists.FindAsync(c.ListId, ct)
                   ?? throw new KeyNotFoundException($"Список с ID=[{c.ListId}] не найден");

        list.RemoveItem(c.ListItemId);
        
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