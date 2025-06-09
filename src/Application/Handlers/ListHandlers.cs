using Application.Commands;
using Application.DTO;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Handlers;

public sealed class CreateListHandler(IUnitOfWork uow, IMapper mapper) : IRequestHandler<CreateListCommand, ShoppingListDto>
{
    public async Task<ShoppingListDto> Handle(CreateListCommand c, CancellationToken ct)
    {
        var user = await uow.Users.FindAsync(c.UserId, ct)
            ?? throw new KeyNotFoundException("User not found");

        var list = new ShoppingList(c.Name, user);
        uow.ShoppingLists.Add(list);
        
        await uow.SaveChangesAsync(ct);
        return mapper.Map<ShoppingListDto>(list);
    }
}

public sealed class RenameListHandler(IUnitOfWork uow, IMapper mapper) : IRequestHandler<RenameListCommand, ShoppingListDto>
{
    public async Task<ShoppingListDto> Handle(RenameListCommand c, CancellationToken ct)
    {
        var list = await uow.ShoppingLists.FindAsync(c.Id, ct)
                   ?? throw new KeyNotFoundException("Shopping list not found");
        
        list.Rename(c.Name);
        await uow.SaveChangesAsync(ct);
        
        return mapper.Map<ShoppingListDto>(list);
    }
}