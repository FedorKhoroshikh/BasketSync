namespace Domain.Entities;

public class DiscountCard
{
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public User User { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Comment { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<CardIdentifier> _identifiers = [];
    public IReadOnlyCollection<CardIdentifier> Identifiers => _identifiers;

    public DiscountCard() { }

    public DiscountCard(User user, string name, string? comment = null)
    {
        User = user;
        UserId = user.Id;
        Name = name;
        Comment = comment;
    }

    public void Update(string name, string? comment)
    {
        Name = name;
        Comment = comment;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public CardIdentifier AddIdentifier(IdentifierType type, string value, string? imagePath = null)
    {
        var identifier = new CardIdentifier(this, type, value, imagePath);
        _identifiers.Add(identifier);
        return identifier;
    }

    public void RemoveIdentifier(int identifierId)
    {
        var identifier = _identifiers.SingleOrDefault(x => x.Id == identifierId);
        if (identifier is null)
            throw new KeyNotFoundException("Identifier not found");

        _identifiers.Remove(identifier);
    }
}
