using Application.Commands;
using Application.DTO;
using Application.Handlers;
using Application.Queries;
using Domain.Entities;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace BasketSyncTests;

[TestFixture]
public class ListHandlersTests : TestBase
{
    [Test]
    public async Task GetAllLists_Test()
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);

        var user = Seed.TestUser();
        var list1 = new ShoppingList("List A", user);
        var list2 = new ShoppingList("List B", user);

        db.AddRange(user, list1, list2);
        await db.SaveChangesAsync(_ct);

        var handler = new GetAllListsHandler(_uow, Mapper);
        var result = await handler.Handle(new GetAllListsQuery(user.Id), _ct);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Select(r => r.Name), Is.EquivalentTo(new[] { "List A", "List B" }));
    }

    [Test]
    public async Task RemoveList_Test()
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);

        var user = Seed.TestUser();
        var list = Seed.TestList(user);

        db.AddRange(user, list);
        await db.SaveChangesAsync(_ct);

        var handler = new RemoveListHandler(_uow);
        var result = await handler.Handle(new RemoveListCommand(list.Id), _ct);

        Assert.That(result, Is.EqualTo(MediatR.Unit.Value));
        Assert.That(await db.ShoppingLists.CountAsync(_ct), Is.EqualTo(0));
    }

    [TestCase("OBI")]
    [TestCase("S-Market")]
    public async Task CreateList_ReturnDto_Test(string listName)
    {
        var user = Seed.TestUser();
        var db = NewDb();
        _uow = new UnitOfWork(db);

        db.Add(user);
        await db.SaveChangesAsync(_ct);

        var cmd = new CreateListCommand(listName, user.Id);
        var handler = new CreateListHandler(_uow, Mapper);

        var dto = await handler.Handle(cmd, _ct);

        Assert.That(dto.Id, Is.EqualTo(1));
        Assert.That(dto.Name, Is.EqualTo(listName));
    }

    [TestCase("New_Name")]
    public async Task RenameList_ReturnDto_Test(string newName)
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);

        var list = Seed.TestList(Seed.TestUser());

        db.Add(list);
        await db.SaveChangesAsync(_ct);

        var cmd = new RenameListCommand(list.Id, newName, true);
        var handler = new RenameListHandler(_uow, Mapper);

        var dto = await handler.Handle(cmd, _ct);

        Assert.That(dto.Name, Is.EqualTo(newName));
        Assert.That((await _uow.ShoppingLists.FindAsync(list.Id, _ct))!.Name, Is.EqualTo(newName));
    }

    [Test]
    public void EmptyNameOnRename_Test()
    {
        var db = NewDb();

        var list = Seed.TestList(Seed.TestUser());

        db.Add(list);
        db.SaveChanges();

        var cmd = new RenameListCommand(list.Id, "", true);
        var handler = new RenameListHandler(new UnitOfWork(db), Mapper);

        Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(cmd, _ct));
    }

    [TestCase(2)]
    public async Task GetList_ReturnDto_Test(int resQty)
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);

        var unit = Seed.TestUnit();
        var cat = Seed.TestCategory();
        var item1 = Seed.TestItem(unit, cat);
        var item2 = Seed.TestItem(unit, cat, "Bananas");
        var list = new ShoppingList("Name", Seed.TestUser());
        var listItem1 = new ListItem(list, item1, 3);
        var listItem2 = new ListItem(list, item2, 8);

        db.AddRange(unit, cat, item1, item2, listItem1, listItem2, list);
        await db.SaveChangesAsync(_ct);

        var qry = new GetListQuery(list.Id);
        var handler = new GetListHandler(_uow, Mapper);

        var dto = await handler.Handle(qry, _ct);

        Assert.That(dto.Name, Is.EqualTo(list.Name));
        Assert.That(list.ListItems, Has.Count.EqualTo(resQty));
    }

    // ── Shared list tests ──

    [Test]
    public async Task GetAllLists_IncludesSharedLists()
    {
        var db = NewDb();
        var userA = Seed.TestUser("Alice");
        var userB = Seed.TestUser("Bob");

        var ownList = new ShoppingList("My List", userA);
        var sharedList = new ShoppingList("Shared List", userB); // shared by default (IsShared=true)
        var privateList = new ShoppingList("Private List", userB);
        privateList.SetShared(false);

        db.AddRange(userA, userB, ownList, sharedList, privateList);
        await db.SaveChangesAsync(_ct);

        _uow = new UnitOfWork(db);
        var handler = new GetAllListsHandler(_uow, Mapper);
        var result = await handler.Handle(new GetAllListsQuery(userA.Id), _ct);

        Assert.That(result.Select(r => r.Name), Is.EquivalentTo(new[] { "My List", "Shared List" }));
    }

    [Test]
    public async Task GetAllLists_ExcludesPrivateLists()
    {
        var db = NewDb();
        var userA = Seed.TestUser("Alice");
        var userB = Seed.TestUser("Bob");

        var privateList = new ShoppingList("Private", userB);
        privateList.SetShared(false);

        db.AddRange(userA, userB, privateList);
        await db.SaveChangesAsync(_ct);

        _uow = new UnitOfWork(db);
        var handler = new GetAllListsHandler(_uow, Mapper);
        var result = await handler.Handle(new GetAllListsQuery(userA.Id), _ct);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task CreateList_DefaultIsShared()
    {
        var db = NewDb();
        var user = Seed.TestUser();
        db.Add(user);
        await db.SaveChangesAsync(_ct);

        _uow = new UnitOfWork(db);
        var handler = new CreateListHandler(_uow, Mapper);
        var dto = await handler.Handle(new CreateListCommand("Groceries", user.Id), _ct);

        Assert.That(dto.IsShared, Is.True);
        var list = await db.ShoppingLists.FirstAsync(_ct);
        Assert.That(list.IsShared, Is.True);
    }

    [Test]
    public async Task RenameList_UpdatesIsShared()
    {
        var db = NewDb();
        var list = Seed.TestList(Seed.TestUser());
        db.Add(list);
        await db.SaveChangesAsync(_ct);

        Assert.That(list.IsShared, Is.True);

        _uow = new UnitOfWork(db);
        var handler = new RenameListHandler(_uow, Mapper);
        var dto = await handler.Handle(new RenameListCommand(list.Id, "Renamed", false), _ct);

        Assert.That(dto.IsShared, Is.False);
        Assert.That(dto.Name, Is.EqualTo("Renamed"));
    }

    [Test]
    public async Task GetAllLists_IncludesSpecificSharedLists()
    {
        var db = NewDb();
        var userA = Seed.TestUser("Alice");
        var userB = Seed.TestUser("Bob");

        // Private list shared specifically with Alice
        var specificList = new ShoppingList("Shared With Alice", userB);
        specificList.SetShared(false);

        db.AddRange(userA, userB, specificList);
        await db.SaveChangesAsync(_ct);

        // Add specific share
        var share = new ListShare(specificList, userA);
        db.Add(share);
        await db.SaveChangesAsync(_ct);

        _uow = new UnitOfWork(db);
        var handler = new GetAllListsHandler(_uow, Mapper);
        var result = await handler.Handle(new GetAllListsQuery(userA.Id), _ct);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Shared With Alice"));
    }

    [Test]
    public async Task UpdateShares_SetsSpecificUsers()
    {
        var db = NewDb();
        var owner = Seed.TestUser("Owner");
        var userA = Seed.TestUser("Alice");
        var userB = Seed.TestUser("Bob");
        var list = new ShoppingList("Shared List", owner);
        list.SetShared(false);

        db.AddRange(owner, userA, userB, list);
        await db.SaveChangesAsync(_ct);

        _uow = new UnitOfWork(db);
        var handler = new UpdateListSharesHandler(_uow);
        await handler.Handle(
            new UpdateListSharesCommand(list.Id, owner.Id, [userA.Id, userB.Id]), _ct);

        var shares = await db.Set<ListShare>().Where(ls => ls.ShoppingListId == list.Id).ToListAsync(_ct);
        Assert.That(shares, Has.Count.EqualTo(2));
        Assert.That(shares.Select(s => s.UserId), Is.EquivalentTo(new[] { userA.Id, userB.Id }));
    }

    [Test]
    public async Task UpdateShares_NonOwner_Throws()
    {
        var db = NewDb();
        var owner = Seed.TestUser("Owner");
        var other = Seed.TestUser("Other");
        var list = new ShoppingList("List", owner);

        db.AddRange(owner, other, list);
        await db.SaveChangesAsync(_ct);

        _uow = new UnitOfWork(db);
        var handler = new UpdateListSharesHandler(_uow);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new UpdateListSharesCommand(list.Id, other.Id, [other.Id]), _ct));
    }
}
