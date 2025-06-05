namespace Domain.Entities;

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    
    public Category Category { get; set; } = null!;
    public int CategoryId { get; set; }
    
    public Unit Unit { get; set; } = null!;
    public int UnitId { get; set; }
    
    public ICollection<ListItem> ListItems { get; set; } = new List<ListItem>();
}