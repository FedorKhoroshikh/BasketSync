using Application.Interfaces;
using Application.Mapping;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BasketSyncTests;

public abstract class TestBase
{
    protected static readonly IMapper Mapper = new MapperConfiguration(c => c.AddProfile<MappingProfile>())
        .CreateMapper();

    protected static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    protected IUnitOfWork _uow = null!;

    protected readonly CancellationToken _ct = CancellationToken.None;

    public static class Seed
    {
        public static User TestUser(string name = "John") => new(name, "123");
        public static User GoogleUser(string name = "GoogleJohn", string email = "john@gmail.com", string googleId = "google-sub-123") => new(name, email, googleId);
        public static ShoppingList TestList(User u, string name = "Groceries") => new(name, u);
        public static Item TestItem(Unit u, Category c, string name = "Apples") => new(name, c, u);
        public static ListItem TestListItem(ShoppingList l, Item i, int quantity = 5) => new(l, i, quantity);
        public static Category TestCategory(string name = "Fruits") => new(name);
        public static Unit TestUnit(string name = "kg") => new(name);
    }
}
