using Application.DTO;
using Application.Interfaces;
using Application.Queries;
using AutoMapper;
using MediatR;

namespace Application.Handlers;

public sealed class GetListHandler(IUnitOfWork uow, IMapper mapper) : IRequestHandler<GetListQuery, ShoppingListDto>
{
    public async Task<ShoppingListDto> Handle(GetListQuery c, CancellationToken ct)
    {
        var list = await uow.ShoppingLists.FindAsync(c.ListId, ct)
            ?? throw new KeyNotFoundException("Shopping list not found");
        
        return mapper.Map<ShoppingListDto>(list);
    }
}