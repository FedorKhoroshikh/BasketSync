using Application.Commands;
using Application.DTO;
using Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BasketSync.WebApi.Controllers;

[ApiController]
[Route("api")]
public class CatalogController(IMediator mediator) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories(CancellationToken ct)
        => Ok(await mediator.Send(new GetAllCategoriesQuery(), ct));

    [HttpGet("units")]
    public async Task<ActionResult<List<UnitDto>>> GetUnits(CancellationToken ct)
        => Ok(await mediator.Send(new GetAllUnitsQuery(), ct));

    [HttpPost("categories")]
    public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryRequest body, CancellationToken ct)
    {
        var dto = await mediator.Send(new CreateCategoryCommand(body.Name), ct);
        return Created($"/api/categories/{dto.Id}", dto);
    }

    [HttpPost("units")]
    public async Task<ActionResult<UnitDto>> CreateUnit([FromBody] CreateUnitRequest body, CancellationToken ct)
    {
        var dto = await mediator.Send(new CreateUnitCommand(body.Name), ct);
        return Created($"/api/units/{dto.Id}", dto);
    }

    public record CreateCategoryRequest(string Name);
    public record CreateUnitRequest(string Name);
}
