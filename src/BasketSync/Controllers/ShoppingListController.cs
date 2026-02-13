using System.Security.Claims;
using Application.Commands;
using Application.DTO;
using Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace BasketSync.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/lists")]
public class ShoppingListController(IMediator mediator) : ControllerBase
{
    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

#region ShoppingList methods

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ListItemDto>> Update(int id, [FromBody] RenameListRequest body, CancellationToken ct)
        => Ok(await mediator.Send(new RenameListCommand(id, body.Name, body.IsShared), ct));

    [HttpPost]
    public async Task<ActionResult<ShoppingListDto>> Create([FromBody] CreateListRequest body, CancellationToken ct)
    {
        var dto = await mediator.Send(new CreateListCommand(body.Name, GetUserId(), body.IsShared), ct);
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
    public async Task<ActionResult> Toggle(int id, CancellationToken ct)
    {
        await mediator.Send(new ToggleListItemByIdCommand(id), ct);
        return NoContent();
    }

    [HttpPatch("{listId:int}/items/{itemId:int}")]
    public async Task<ActionResult<ListItemDto>> UpdateItem(int listId, int itemId, [FromBody] UpdateListItemRequest body, CancellationToken ct)
    {
        var dto = await mediator.Send(new UpdateListItemCommand(itemId, body.Quantity, body.Comment, body.CategoryId, body.UnitId), ct);
        return Ok(dto);
    }

#endregion

#region ListItem requests

    public record AddListItemRequest(int ItemId, int Quantity);
    public record UpdateListItemRequest(int? Quantity = null, string? Comment = null, int? CategoryId = null, int? UnitId = null);

#endregion

#region Shares

    [HttpGet("{id:int}/shares")]
    public async Task<ActionResult<int[]>> GetShares(int id, CancellationToken ct)
        => Ok(await mediator.Send(new GetListSharesQuery(id), ct));

    [HttpPut("{id:int}/shares")]
    public async Task<ActionResult> UpdateShares(int id, [FromBody] UpdateSharesRequest body, CancellationToken ct)
    {
        await mediator.Send(new UpdateListSharesCommand(id, GetUserId(), body.UserIds), ct);
        return NoContent();
    }

#endregion

#region ShoppingList requests

    public record RemoveListItemRequest(int ItemId);
    public record RenameListRequest(string Name, bool IsShared);
    public record CreateListRequest(string Name, bool IsShared = true);
    public record UpdateSharesRequest(int[] UserIds);

#endregion

}
