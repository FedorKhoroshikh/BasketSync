using Application.Commands;
using Application.DTO;
using Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BasketSync.WebApi.Controllers;

[ApiController]
[Route("api/items")]
public class ItemController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ItemDto>>> Search([FromQuery] string? search, CancellationToken ct)
        => Ok(await mediator.Send(new SearchItemsQuery(search ?? ""), ct));

    [HttpPost]
    public async Task<ActionResult<ItemDto>> Create([FromBody] CreateItemRequest body, CancellationToken ct)
    {
        var dto = await mediator.Send(new CreateItemCommand(body.Name, body.CategoryId, body.UnitId), ct);
        return Created($"/api/items/{dto.Id}", dto);
    }

    public record CreateItemRequest(string Name, int CategoryId, int UnitId);
}
