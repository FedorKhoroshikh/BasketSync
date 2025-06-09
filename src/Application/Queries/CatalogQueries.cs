using MediatR;
using Application.DTO;

namespace Application.Queries;

public sealed record GetCategoriesQuery(int CategoryId) : IRequest<CategoryDto>;

public class GetUnitQuery(int UnitId) : IRequest<UnitDto>;