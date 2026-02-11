namespace Domain.Entities;

public class CardIdentifier
{
    public int Id { get; private set; }
    public int DiscountCardId { get; private set; }
    public DiscountCard DiscountCard { get; private set; } = null!;
    public IdentifierType Type { get; private set; }
    public string Value { get; private set; } = null!;
    public string? ImagePath { get; private set; }

    public CardIdentifier() { }

    public CardIdentifier(DiscountCard card, IdentifierType type, string value, string? imagePath = null)
    {
        DiscountCard = card;
        DiscountCardId = card.Id;
        Type = type;
        Value = value;
        ImagePath = imagePath;
    }
}
