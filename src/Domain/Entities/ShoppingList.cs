namespace Domain.Entities;

public class ShoppingList
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int UserId { get; set; }
    
    public User User { get; set; } = null!;
    public ICollection<ListItem> ListItems { get; set; } = new List<ListItem>();
}