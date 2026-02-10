using Application.Commands;
using Application.DTO;
using Application.Handlers;
using Domain.Entities;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace BasketSyncTests;

[TestFixture]
public class ItemHandlersTests : TestBase
{
    [Test]
    public async Task RemoveItem_ReturnDto_Test()
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);

        var unit = Seed.TestUnit();
        var cat = Seed.TestCategory();
        var item1 = Seed.TestItem(unit, cat);
        var item2 = Seed.TestItem(unit, cat, "Bananas");
        var list = Seed.TestList(Seed.TestUser());
        var li1 = Seed.TestListItem(list, item1);
        var li2 = Seed.TestListItem(list, item2);

        db.AddRange(unit, cat, item1, item2, list, li1, li2);
        await db.SaveChangesAsync(_ct);

        var cmd = new RemoveItemCommand(list.Id, li2.Id);
        var handler = new RemoveItemHandler(_uow);

        var result = await handler.Handle(cmd, _ct);

        Assert.That(list.ListItems, Has.Count.EqualTo(1));
        Assert.That(result, Is.EqualTo(MediatR.Unit.Value));
    }

    [TestCase(2)]
    [TestCase(10)]
    public async Task AddItem_ReturnDto_Test(int quantity)
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);

        var unit = Seed.TestUnit();
        var category = Seed.TestCategory();
        var item = Seed.TestItem(unit, category);
        var list = new ShoppingList("Name", Seed.TestUser());

        db.AddRange(list, category, unit, item);
        await db.SaveChangesAsync(_ct);

        var cmd = new AddListItemCommand(list.Id, item.Id, quantity);
        var handler = new AddItemHandler(_uow, Mapper);

        ListItemDto dto = await handler.Handle(cmd, _ct);

        Assert.That(dto.ItemId, Is.EqualTo(item.Id));
        Assert.That(dto.Quantity, Is.EqualTo(quantity));
        Assert.That(dto.IsChecked, Is.False);
        Assert.That(await db.ShoppingLists.CountAsync(_ct), Is.EqualTo(1));
    }

    [Test]
    public async Task ToggleItem_ReturnDto_Test()
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);

        var cat = Seed.TestCategory();
        var unit = Seed.TestUnit();
        var item = Seed.TestItem(unit, cat);
        var list = Seed.TestList(Seed.TestUser());
        var listItem = Seed.TestListItem(list, item);

        db.AddRange(list, cat, unit, item, listItem);
        await db.SaveChangesAsync(_ct);

        var cmd = new ToggleItemCommand(list.Id, listItem.Id);
        var handler = new ToggleItemHandler(_uow);

        var result = await handler.Handle(cmd, _ct);
        var toggle = await _uow.ListItems.FindAsync(item.Id, _ct);

        Assert.That(toggle is { IsChecked: true }, Is.True);
        Assert.That(result, Is.EqualTo(MediatR.Unit.Value));
    }

    [Test]
    public void ListNotFound_Test()
    {
        using var db = NewDb();
        var handler = new AddItemHandler(new UnitOfWork(db), Mapper);

        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(new AddListItemCommand(ListId: 42, ItemId: 1, Quantity: 1), _ct));
    }
}
