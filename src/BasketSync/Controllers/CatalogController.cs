using Application.Commands;
using Application.DTO;
using Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BasketSync.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class CatalogController(IMediator mediator) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories(CancellationToken ct)
        => Ok(await mediator.Send(new GetAllCategoriesQuery(), ct));

    [HttpGet("units")]
    public async Task<ActionResult<List<UnitDto>>> GetUnits(CancellationToken ct)
        => Ok(await mediator.Send(new GetAllUnitsQuery(), ct));

    [HttpGet("categories/{id:int}")]
    public async Task<ActionResult<CategoryDto>> GetCategory(int id, CancellationToken ct)
        => Ok(await mediator.Send(new GetCategoryByIdQuery(id), ct));

    [HttpPost("categories")]
    public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryRequest body, CancellationToken ct)
    {
        var dto = await mediator.Send(new CreateCategoryCommand(body.Name, body.Comment), ct);
        return Created($"/api/categories/{dto.Id}", dto);
    }

    [HttpPut("categories/{id:int}")]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(int id, [FromBody] UpdateCategoryRequest body, CancellationToken ct)
        => Ok(await mediator.Send(new UpdateCategoryCommand(id, body.Name, body.Comment), ct));

    [HttpDelete("categories/{id:int}")]
    public async Task<ActionResult> DeleteCategory(int id, CancellationToken ct)
    {
        await mediator.Send(new DeleteCategoryCommand(id), ct);
        return NoContent();
    }

    [HttpPost("units")]
    public async Task<ActionResult<UnitDto>> CreateUnit([FromBody] CreateUnitRequest body, CancellationToken ct)
    {
        var dto = await mediator.Send(new CreateUnitCommand(body.Name), ct);
        return Created($"/api/units/{dto.Id}", dto);
    }

    public record CreateCategoryRequest(string Name, string? Comment = null);
    public record UpdateCategoryRequest(string Name, string? Comment);
    public record CreateUnitRequest(string Name);
}
