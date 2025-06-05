namespace Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string PwdHash { get; set; } = null!;
    
    public ICollection<ShoppingList> Lists { get; set; } = new List<ShoppingList>();
}