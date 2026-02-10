using Application.Commands;
using Application.Handlers;
using Application.Queries;
using Domain.Entities;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace BasketSyncTests;

[TestFixture]
public class CatalogHandlersTests : TestBase
{
    [Test]
    public async Task GetAllCategories_Test()
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);

        db.AddRange(new Category("Овощи"), new Category("Фрукты"), new Category("Молочная"));
        await db.SaveChangesAsync(_ct);

        var handler = new GetAllCategoriesHandler(_uow, Mapper);
        var result = await handler.Handle(new GetAllCategoriesQuery(), _ct);

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[0].Name, Is.EqualTo("Молочная"));
    }

    [Test]
    public async Task GetAllUnits_Test()
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);

        db.AddRange(Seed.TestUnit("шт"), Seed.TestUnit("кг"), Seed.TestUnit("л"));
        await db.SaveChangesAsync(_ct);

        var handler = new GetAllUnitsHandler(_uow, Mapper);
        var result = await handler.Handle(new GetAllUnitsQuery(), _ct);

        Assert.That(result, Has.Count.EqualTo(3));
    }

    [TestCase("Напитки")]
    public async Task CreateCategory_Test(string name)
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);

        var handler = new CreateCategoryHandler(_uow, Mapper);
        var dto = await handler.Handle(new CreateCategoryCommand(name), _ct);

        Assert.That(dto.Name, Is.EqualTo(name));
        Assert.That(await db.Categories.CountAsync(_ct), Is.EqualTo(1));
    }

    [TestCase("г")]
    public async Task CreateUnit_Test(string name)
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);

        var handler = new CreateUnitHandler(_uow, Mapper);
        var dto = await handler.Handle(new CreateUnitCommand(name), _ct);

        Assert.That(dto.Name, Is.EqualTo(name));
        Assert.That(await db.Units.CountAsync(_ct), Is.EqualTo(1));
    }

    [Test]
    public async Task SearchItems_ByName_Test()
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);

        var cat = Seed.TestCategory();
        var unit = Seed.TestUnit();
        var item1 = Seed.TestItem(unit, cat, "Яблоко");
        var item2 = Seed.TestItem(unit, cat, "Молоко");

        db.AddRange(cat, unit, item1, item2);
        await db.SaveChangesAsync(_ct);

        var handler = new SearchItemsHandler(_uow);
        var result = await handler.Handle(new SearchItemsQuery("молок"), _ct);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Молоко"));
    }

    [Test]
    public async Task SearchItems_EmptyQuery_ReturnsAll_Test()
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);

        var cat = Seed.TestCategory();
        var unit = Seed.TestUnit();
        db.AddRange(cat, unit, Seed.TestItem(unit, cat, "A"), Seed.TestItem(unit, cat, "B"));
        await db.SaveChangesAsync(_ct);

        var handler = new SearchItemsHandler(_uow);
        var result = await handler.Handle(new SearchItemsQuery(""), _ct);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task CreateItem_Test()
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);

        var cat = Seed.TestCategory();
        var unit = Seed.TestUnit();
        db.AddRange(cat, unit);
        await db.SaveChangesAsync(_ct);

        var handler = new CreateItemHandler(_uow, Mapper);
        var dto = await handler.Handle(new CreateItemCommand("Банан", cat.Id, unit.Id), _ct);

        Assert.That(dto.Name, Is.EqualTo("Банан"));
        Assert.That(dto.CategoryId, Is.EqualTo(cat.Id));
        Assert.That(dto.UnitId, Is.EqualTo(unit.Id));
        Assert.That(await db.Items.CountAsync(_ct), Is.EqualTo(1));
    }

    [Test]
    public void CreateItem_CategoryNotFound_Test()
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);

        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            new CreateItemHandler(_uow, Mapper)
                .Handle(new CreateItemCommand("Test", 99, 99), _ct));
    }
}
