using Application.Commands;
using Application.DTO;
using Application.Interfaces;
using AutoMapper;
using MediatR;

namespace Application.Handlers;

public sealed class AddItemHandler(IUnitOfWork uow, IMapper mapper) : IRequestHandler<AddItemCommand, ListItemDto>
{
    public async Task<ListItemDto> Handle(AddItemCommand c, CancellationToken ct)
    {
        var list = await uow.ShoppingLists.FindAsync(c.ListId, ct)
            ?? throw new KeyNotFoundException("Shopping list not found");

        var item = await uow.Items.FindAsync(c.ItemId, ct)
            ?? throw new KeyNotFoundException("Item not found");
        
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
                   ?? throw new KeyNotFoundException("Shopping list not found");

        list.RemoveItem(c.ListItemId);
        
        await uow.SaveChangesAsync(ct);
        
        return Unit.Value;
    }
}

public sealed class ToggleItemHandler(IUnitOfWork uow) : IRequestHandler<ToggleItemCommand, Unit>
{
    public async Task<Unit> Handle(ToggleItemCommand c, CancellationToken ct)
    {
        var list = await uow.ShoppingLists.FindAsync(c.ItemId, ct)
                   ?? throw new KeyNotFoundException("Shopping list not found");

        var listItem = list.ListItems.Single(li => li.Id == c.ItemId);
        listItem.Toggle();

        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}