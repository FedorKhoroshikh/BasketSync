using Domain.Entities;
using Infrastructure.Data;
using Application.Interfaces;

namespace Infrastructure.Services;

public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public IRepository<ShoppingList> ShoppingLists => new EfRepository<ShoppingList>(db);
    public IRepository<Item>         Items         => new EfRepository<Item>(db);
    public IRepository<ListItem>     ListItems     => new EfRepository<ListItem>(db);
    public IRepository<User>         Users         => new EfRepository<User>(db);
    public IRepository<Category>     Categories    => new EfRepository<Category>(db);
    public IRepository<Unit>         Units         => new EfRepository<Unit>(db);
    public IRepository<DiscountCard>   DiscountCards   => new EfRepository<DiscountCard>(db);
    public IRepository<CardIdentifier> CardIdentifiers => new EfRepository<CardIdentifier>(db);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}