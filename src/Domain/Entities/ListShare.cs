namespace Domain.Entities;

public class ListShare
{
    public int Id { get; private set; }
    public int ShoppingListId { get; private set; }
    public ShoppingList ShoppingList { get; private set; } = null!;
    public int UserId { get; private set; }
    public User User { get; private set; } = null!;

    public ListShare() { }

    public ListShare(ShoppingList list, User user)
        => (ShoppingList, User) = (list, user);
}
