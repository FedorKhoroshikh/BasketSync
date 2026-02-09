using Application.Commands;
using Application.DTO;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BasketSync.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResultDto>> Register([FromBody] AuthRequest body, CancellationToken ct)
    {
        var dto = await mediator.Send(new RegisterCommand(body.Name, body.Password), ct);
        return Ok(dto);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResultDto>> Login([FromBody] AuthRequest body, CancellationToken ct)
    {
        var dto = await mediator.Send(new LoginCommand(body.Name, body.Password), ct);
        return Ok(dto);
    }

    public record AuthRequest(string Name, string Password);
}
