using Application.Commands;
using Application.DTO;
using Application.Queries;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace BasketSync.WebApi.Controllers;

[ApiController]
[Route("api/lists")]
public class ShoppingListController(IMediator mediator) : ControllerBase
{

#region ShoppingList methods
    
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ListItemDto>> Update(int id, [FromBody] RenameListRequest body, CancellationToken ct) 
        => Ok(await mediator.Send(new RenameListCommand(id, body.Name), ct));
    
    [HttpPost]
    public async Task<ActionResult<ShoppingListDto>> Create([FromBody] CreateListRequest body, CancellationToken ct)
    {
        var dto = await mediator.Send(new CreateListCommand(body.Name, body.UserId), ct);
        return CreatedAtAction(nameof(Get), new {id = dto.Id}, dto);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ShoppingListDto>> Delete(int id, CancellationToken ct)
    {
        await mediator.Send(new RemoveListCommand(id), ct);
        return NoContent();
    }
    
#endregion
    
#region ListItems methods

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ShoppingListDto>> Get(int id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetListQuery(id), ct));

    [HttpPost("{listId:int}/items")]
    public async Task<ActionResult<ListItemDto>> AddItem(int listId, [FromBody] AddListItemRequest body, CancellationToken ct)
    {
        var dto = await mediator.Send(
            new AddListItemCommand(listId, body.ItemId, body.Quantity), ct);
        return CreatedAtAction(nameof(Get), new {id = listId}, dto);
    }

    
    [HttpDelete("{listId:int}/items")]
    public async Task<ActionResult> DeleteItem(int listId, [FromBody] RemoveListItemRequest body, CancellationToken ct)
    {
        await mediator.Send(new RemoveItemCommand(listId, body.ItemId), ct);
        return NoContent();
    }
    
    [HttpPatch("/api/items/{id:int}/toggle")]
    public async Task<ActionResult<ListItem>> Toggle(int listId, [FromBody] ToggleListItemRequest body, CancellationToken ct)
    {
        await mediator.Send(new ToggleItemCommand(listId, body.ItemId), ct);
        return NoContent();
    }

#endregion    
    
#region ListItem requests

    public record AddListItemRequest(int ItemId, int Quantity);
    public record ToggleListItemRequest(int ItemId);
    
#endregion

#region ShoppingList requests

    public record RemoveListItemRequest(int ItemId);
    public record RenameListRequest(string Name);
    public record CreateListRequest(string Name, int UserId);
    
#endregion

}
