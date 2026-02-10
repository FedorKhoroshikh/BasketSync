using Application.Commands;
using Application.DTO;
using Application.Exceptions;
using Application.Interfaces;
using Application.Queries;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public sealed class GetAllCategoriesHandler(IUnitOfWork uow, IMapper mapper)
    : IRequestHandler<GetAllCategoriesQuery, List<CategoryDto>>
{
    public async Task<List<CategoryDto>> Handle(GetAllCategoriesQuery request, CancellationToken ct)
    {
        return await uow.Categories
            .GetAll()
            .OrderBy(c => c.Name)
            .ProjectTo<CategoryDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);
    }
}

public sealed class GetAllUnitsHandler(IUnitOfWork uow, IMapper mapper)
    : IRequestHandler<GetAllUnitsQuery, List<UnitDto>>
{
    public async Task<List<UnitDto>> Handle(GetAllUnitsQuery request, CancellationToken ct)
    {
        return await uow.Units
            .GetAll()
            .OrderBy(u => u.Name)
            .ProjectTo<UnitDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);
    }
}

public sealed class CreateCategoryHandler(IUnitOfWork uow, IMapper mapper)
    : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(CreateCategoryCommand c, CancellationToken ct)
    {
        try
        {
            var category = new Category(c.Name);
            uow.Categories.Add(category);
            await uow.SaveChangesAsync(ct);
            return mapper.Map<CategoryDto>(category);
        }
        catch (DbUpdateException ex) when (ConflictException.IsUniqueViolation(ex))
        {
            throw new ConflictException($"Категория '{c.Name}' уже существует");
        }
    }
}

public sealed class CreateUnitHandler(IUnitOfWork uow, IMapper mapper)
    : IRequestHandler<CreateUnitCommand, UnitDto>
{
    public async Task<UnitDto> Handle(CreateUnitCommand c, CancellationToken ct)
    {
        try
        {
            var unit = new Domain.Entities.Unit(c.Name);
            uow.Units.Add(unit);
            await uow.SaveChangesAsync(ct);
            return mapper.Map<UnitDto>(unit);
        }
        catch (DbUpdateException ex) when (ConflictException.IsUniqueViolation(ex))
        {
            throw new ConflictException($"Единица измерения '{c.Name}' уже существует");
        }
    }
}

public sealed class SearchItemsHandler(IUnitOfWork uow)
    : IRequestHandler<SearchItemsQuery, List<ItemDto>>
{
    public async Task<List<ItemDto>> Handle(SearchItemsQuery request, CancellationToken ct)
    {
        var query = uow.Items.GetAll()
            .Include(i => i.Category)
            .Include(i => i.Unit)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var search = request.Name.ToLower();
            query = query.Where(i => i.Name.ToLower().Contains(search));
        }

        return await query
            .OrderBy(i => i.Name)
            .Select(i => new ItemDto(
                i.Id, i.Name, i.CategoryId, i.UnitId,
                i.Category.Name, i.Unit.Name))
            .ToListAsync(ct);
    }
}

public sealed class CreateItemHandler(IUnitOfWork uow, IMapper mapper)
    : IRequestHandler<CreateItemCommand, ItemDto>
{
    public async Task<ItemDto> Handle(CreateItemCommand c, CancellationToken ct)
    {
        var category = await uow.Categories.FindAsync(c.CategoryId, ct)
                       ?? throw new KeyNotFoundException($"Категория с ID=[{c.CategoryId}] не найдена");

        var unit = await uow.Units.FindAsync(c.UnitId, ct)
                   ?? throw new KeyNotFoundException($"Единица измерения с ID=[{c.UnitId}] не найдена");

        try
        {
            var item = new Item(c.Name, category, unit);
            uow.Items.Add(item);
            await uow.SaveChangesAsync(ct);
            return mapper.Map<ItemDto>(item);
        }
        catch (DbUpdateException ex) when (ConflictException.IsUniqueViolation(ex))
        {
            throw new ConflictException($"Товар '{c.Name}' уже существует");
        }
    }
}
