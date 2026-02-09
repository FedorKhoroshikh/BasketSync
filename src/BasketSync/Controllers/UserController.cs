using System.Security.Claims;
using Application.DTO;
using Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BasketSync.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UserController(IMediator mediator) : ControllerBase
{
    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("me/lists")]
    public async Task<ActionResult<IEnumerable<ShoppingListDto>>> GetMyLists(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllListsQuery(GetUserId()), ct);
        return Ok(result);
    }

    [HttpGet("{userId:int}/lists")]
    public async Task<ActionResult<IEnumerable<ShoppingListDto>>> GetUserLists(int userId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllListsQuery(userId), ct);
        return Ok(result);
    }
}
