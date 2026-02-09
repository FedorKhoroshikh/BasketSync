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
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}