namespace Domain.Entities;

public class ListItem
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public int ListId { get; set; }
    
    public int Quantity { get; set; }
    public bool IsChecked { get; set; }

    public Item Item { get; set; } = null!;
    public ShoppingList List { get; set; } = null!;
}