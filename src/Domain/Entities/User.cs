namespace Domain.Entities;

public class User
{
    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string PwdHash { get; private set; } = null!;
    
    private readonly List<ShoppingList> _lists= [];
    public IReadOnlyList<ShoppingList> Lists => _lists;

    private readonly List<DiscountCard> _discountCards = [];
    public IReadOnlyCollection<DiscountCard> DiscountCards => _discountCards;

    public User() { }
    
    public User(string name, string pwdHash) 
        => (Name, PwdHash) = (name, pwdHash);
}