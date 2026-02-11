using Domain.Entities;

namespace Application.Interfaces;

public interface IUnitOfWork
{
    IRepository<ShoppingList> ShoppingLists { get; }
    IRepository<Item>         Items         { get; }
    IRepository<ListItem>     ListItems     { get; }
    IRepository<User>         Users         { get; }
    IRepository<Category>     Categories    { get; }
    IRepository<Unit>         Units         { get; }
    IRepository<DiscountCard>   DiscountCards   { get; }
    IRepository<CardIdentifier> CardIdentifiers { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}