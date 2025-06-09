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
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ShoppingListDto>> Get(int id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetListQuery(id), ct));

    [HttpPost("{listId:int}/items")]
    public async Task<ActionResult<ListItemDto>> AddItem(int listId, [FromBody] AddItemRequest body, CancellationToken ct)
    {
        var dto = await mediator.Send(
            new AddItemCommand(listId, body.ItemId, body.Quantity), ct);
        return CreatedAtAction(nameof(Get), new {id = listId}, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ListItemDto>> Update(int id, [FromBody] RenameListRequest body, CancellationToken ct) 
        => Ok(await mediator.Send(new RenameListCommand(id, body.Name), ct));
    
    [HttpDelete("{listId:int}/items")]
    public async Task<ActionResult> DeleteItem(int listId, [FromBody] RemoveItemRequest body, CancellationToken ct)
    {
        await mediator.Send(new RemoveItemCommand(listId, body.ItemId), ct);
        return NoContent();
    }
    
    [HttpPatch("/api/items/{id:int}/toggle")]
    public async Task<ActionResult<ListItem>> Toggle(int id, CancellationToken ct)
    {
        await mediator.Send(new ToggleItemCommand(id), ct);
        return NoContent();
    }

    public record AddItemRequest(int ItemId, int Quantity);
    public record RemoveItemRequest(int ItemId);
    public record RenameListRequest(string Name);
}
