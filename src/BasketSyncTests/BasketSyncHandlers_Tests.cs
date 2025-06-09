using Application.Commands;
using Application.DTO;
using Application.Handlers;
using Application.Interfaces;
using Application.Mapping;
using Application.Queries;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Services;

namespace BasketSyncTests
{
    [TestFixture]
    public class BasketSyncHandlersTests
    {
        private static readonly IMapper Mapper = new MapperConfiguration(c => c.AddProfile<MappingProfile>())
            .CreateMapper();

        private static AppDbContext NewDb() =>
            new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        
        private IUnitOfWork _uow;
        
        private readonly CancellationToken  _ct     = CancellationToken.None;
        
        // ---------- Data sources testing ----------
        public static class Seed
        {
            public static User TestUser(string name = "John") => new(name, "123");        
            public static ShoppingList TestList(User u, string name = "Groceries") => new(name, u);
            public static Item TestItem(Unit u, Category c, string name = "Apples") => new(name, c, u);
            public static ListItem TestListItem(ShoppingList l, Item i, int quantity = 5) => new(l, i, quantity);
            public static Category TestCategory(string name = "Fruits") => new(name);
            public static Unit TestUnit(string name = "kg") => new(name);
        }

        // ---------- Unit tests ----------

        [TestCase("OBI")]
        [TestCase("S-Market")]
        public async Task CreateList_ReturnDto_Test(string testName)
        {
            var user = Seed.TestUser();
            var db = NewDb();
            _uow = new UnitOfWork(db);
            
            db.Add(user);
            await db.SaveChangesAsync(_ct);

            var cmd = new CreateListCommand(testName, user.Id);
            var handler = new CreateListHandler(_uow, Mapper);

            var dto = await handler.Handle(cmd, _ct);
            
            Assert.That(dto.Id, Is.EqualTo(1));
        }
        
        [TestCase("New_Name")]
        public async Task RenameList_ReturnDto_Test(string newName)
        {
            var db = NewDb();
            _uow = new UnitOfWork(db);
            
            var list = Seed.TestList(Seed.TestUser());
            
            db.Add(list);
            await db.SaveChangesAsync(_ct);
            
            var cmd = new RenameListCommand(list.Id, newName);
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
            
            var cmd = new RenameListCommand(list.Id, "");
            var handler = new RenameListHandler(new UnitOfWork(db), Mapper);
            
            Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(cmd, _ct));
        }
        
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

        [TestCase(2)]
        [TestCase(10)]
        public async Task AddItem_ReturnDto_Test(int quantity)
        {
            var db = NewDb();
            _uow = new UnitOfWork(db);
            
            // Arrange
            var unit = Seed.TestUnit();
            var category = Seed.TestCategory();
            var item = Seed.TestItem(unit, category);
            var list = new ShoppingList("Name", Seed.TestUser());
            
            db.AddRange(list, category, unit, item);
            await db.SaveChangesAsync(_ct);
            
            var cmd = new AddItemCommand(list.Id, item.Id, quantity);
            var handler = new AddItemHandler(_uow, Mapper);
            
            // Act
            ListItemDto dto = await handler.Handle(cmd, _ct);
            
            // Assert
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
            
            // Arrange 
            var cat = Seed.TestCategory();
            var unit = Seed.TestUnit();
            var item = Seed.TestItem(unit, cat);
            var list = Seed.TestList(Seed.TestUser());
            var listItem = Seed.TestListItem(list, item);
            
            db.AddRange(list, cat, unit, item, listItem);
            await db.SaveChangesAsync(_ct);
            
            var cmd = new ToggleItemCommand(listItem.Id);
            var handler = new ToggleItemHandler(_uow);
            
            // Act
            var result = await handler.Handle(cmd, _ct);
            var toggle = await _uow.ListItems.FindAsync(item.Id, _ct);
            
            // Assert
            Assert.That(toggle is { IsChecked: true }, Is.True);
            Assert.That(result, Is.EqualTo(MediatR.Unit.Value));
        }

        [Test]
        public void ListNotFound_Test()
        {
            using var db = NewDb();
            var handler = new AddItemHandler(new UnitOfWork(db), Mapper);
            
            Assert.ThrowsAsync<KeyNotFoundException>(() => 
                handler.Handle(new AddItemCommand(ListId: 42, ItemId: 1, Quantity: 1), _ct));
        }
    }
}