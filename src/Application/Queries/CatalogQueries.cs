using MediatR;
using Application.DTO;

namespace Application.Queries;

public sealed record GetAllCategoriesQuery() : IRequest<List<CategoryDto>>;

public sealed record GetCategoryByIdQuery(int Id) : IRequest<CategoryDto>;

public sealed record GetAllUnitsQuery() : IRequest<List<UnitDto>>;

public sealed record SearchItemsQuery(string Name) : IRequest<List<ItemDto>>;
