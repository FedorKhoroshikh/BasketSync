namespace Domain.Entities;

public class ListItem
{
    public int Id { get; set; }
    
    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;
    
    public int Quantity { get; set; }
    public bool IsChecked { get; set; }

    public int ListId { get; private set; }
    public ShoppingList List { get; private set; } = null!;

    public ListItem() { }
    public ListItem(ShoppingList list, Item item, int quantity)
    { 
        List = list;
        Item = item;
        IsChecked = false;
        Quantity = quantity;
    }
    
    public void Toggle() => IsChecked = !IsChecked;
}
