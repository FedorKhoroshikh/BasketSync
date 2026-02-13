using System.Security.Claims;
using Application.Commands;
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

    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetProfile(CancellationToken ct)
    {
        var dto = await mediator.Send(new GetUserProfileQuery(GetUserId()), ct);
        return Ok(dto);
    }

    [HttpPut("me/name")]
    public async Task<ActionResult<UserProfileDto>> UpdateName([FromBody] UpdateNameRequest body, CancellationToken ct)
    {
        var dto = await mediator.Send(new UpdateUserNameCommand(GetUserId(), body.Name), ct);
        return Ok(dto);
    }

    [HttpPut("me/email")]
    public async Task<ActionResult<UserProfileDto>> UpdateEmail([FromBody] UpdateEmailRequest body, CancellationToken ct)
    {
        var dto = await mediator.Send(new UpdateUserEmailCommand(GetUserId(), body.Email), ct);
        return Ok(dto);
    }

    [HttpPut("me/password")]
    public async Task<ActionResult<UserProfileDto>> ChangePassword([FromBody] ChangePasswordRequest body, CancellationToken ct)
    {
        if (body.Password != body.ConfirmPassword)
            return BadRequest("Пароли не совпадают");

        var dto = await mediator.Send(new ChangePasswordCommand(GetUserId(), body.Password), ct);
        return Ok(dto);
    }

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

    public record UpdateNameRequest(string Name);
    public record UpdateEmailRequest(string? Email);
    public record ChangePasswordRequest(string Password, string ConfirmPassword);
}
