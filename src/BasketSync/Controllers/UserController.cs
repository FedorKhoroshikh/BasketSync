using Application.DTO;
using Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BasketSync.WebApi.Controllers;

[ApiController]
[Route("api/users")]
public class UserController(IMediator mediator) : ControllerBase
{
    [HttpGet("{userId:int}/lists")]
    public async Task<ActionResult<IEnumerable<ShoppingListDto>>> GetUserLists(int userId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllListsQuery(userId), ct);
        return Ok(result);
    }
}