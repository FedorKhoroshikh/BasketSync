namespace Domain.Entities;

public class User
{
    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? PwdHash { get; private set; }
    public string? Email { get; private set; }
    public string? GoogleId { get; private set; }

    private readonly List<ShoppingList> _lists= [];
    public IReadOnlyList<ShoppingList> Lists => _lists;

    private readonly List<DiscountCard> _discountCards = [];
    public IReadOnlyCollection<DiscountCard> DiscountCards => _discountCards;

    public User() { }

    public User(string name, string pwdHash)
        => (Name, PwdHash) = (name, pwdHash);

    public User(string name, string email, string googleId)
        => (Name, Email, GoogleId) = (name, email, googleId);

    public void LinkGoogle(string email, string googleId)
        => (Email, GoogleId) = (email, googleId);

    public void SetName(string name) => Name = name;
    public void SetEmail(string? email) => Email = email;
    public void SetPwdHash(string pwdHash) => PwdHash = pwdHash;
}