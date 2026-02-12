using System.Security.Claims;
using Application.Commands;
using Application.DTO;
using Application.Interfaces;
using Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BasketSync.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class DiscountCardController(IMediator mediator, IFileStorageService fileStorage) : ControllerBase
{
    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("users/me/cards")]
    public async Task<ActionResult<List<DiscountCardDto>>> GetMyCards(CancellationToken ct)
        => Ok(await mediator.Send(new GetUserCardsQuery(GetUserId()), ct));

    [HttpGet("cards/{id:int}")]
    public async Task<ActionResult<DiscountCardDto>> GetCard(int id, CancellationToken ct)
        => Ok(await mediator.Send(new GetCardByIdQuery(id), ct));

    [HttpPost("cards")]
    public async Task<ActionResult<DiscountCardDto>> CreateCard([FromBody] CreateCardRequest body, CancellationToken ct)
    {
        var dto = await mediator.Send(new CreateDiscountCardCommand(GetUserId(), body.Name, body.Comment, body.IsShared), ct);
        return Created($"/api/cards/{dto.Id}", dto);
    }

    [HttpPut("cards/{id:int}")]
    public async Task<ActionResult<DiscountCardDto>> UpdateCard(int id, [FromBody] UpdateCardRequest body, CancellationToken ct)
        => Ok(await mediator.Send(new UpdateDiscountCardCommand(id, body.Name, body.Comment, body.IsShared), ct));

    [HttpDelete("cards/{id:int}")]
    public async Task<ActionResult> DeleteCard(int id, CancellationToken ct)
    {
        await mediator.Send(new DeleteDiscountCardCommand(id), ct);
        return NoContent();
    }

    [HttpPatch("cards/{id:int}/toggle")]
    public async Task<ActionResult> ToggleCard(int id, CancellationToken ct)
    {
        await mediator.Send(new ToggleDiscountCardCommand(id), ct);
        return NoContent();
    }

    [HttpPost("cards/{id:int}/identifiers")]
    public async Task<ActionResult<CardIdentifierDto>> AddIdentifier(
        int id,
        [FromForm] int type,
        [FromForm] string? value,
        IFormFile? image,
        CancellationToken ct)
    {
        string? imagePath = null;
        if (image is { Length: > 0 })
        {
            await using var stream = image.OpenReadStream();
            imagePath = await fileStorage.SaveAsync(stream, image.FileName, ct);
        }

        var dto = await mediator.Send(new AddCardIdentifierCommand(id, type, value, imagePath), ct);
        return Created($"/api/identifiers/{dto.Id}", dto);
    }

    [HttpPut("identifiers/{id:int}")]
    public async Task<ActionResult<CardIdentifierDto>> UpdateIdentifier(
        int id,
        [FromForm] int type,
        [FromForm] string? value,
        IFormFile? image,
        [FromForm] bool keepImage,
        CancellationToken ct)
    {
        string? newImagePath = null;
        if (image is { Length: > 0 })
        {
            await using var stream = image.OpenReadStream();
            newImagePath = await fileStorage.SaveAsync(stream, image.FileName, ct);
        }

        var dto = await mediator.Send(
            new UpdateCardIdentifierCommand(id, type, value, newImagePath, keepImage && newImagePath is null), ct);
        return Ok(dto);
    }

    [HttpDelete("identifiers/{id:int}")]
    public async Task<ActionResult> RemoveIdentifier(int id, CancellationToken ct)
    {
        await mediator.Send(new RemoveCardIdentifierCommand(id), ct);
        return NoContent();
    }

    [HttpPost("cards/resolve")]
    public async Task<ActionResult<DiscountCardDto>> ResolveCard([FromBody] ResolveCardRequest body, CancellationToken ct)
        => Ok(await mediator.Send(new ResolveCardCommand(body.Value), ct));

    public record CreateCardRequest(string Name, string? Comment, bool IsShared = false);
    public record UpdateCardRequest(string Name, string? Comment, bool IsShared = false);
    public record ResolveCardRequest(string Value);
}
