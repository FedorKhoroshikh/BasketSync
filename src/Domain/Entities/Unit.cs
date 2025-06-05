namespace Domain.Entities;

public class Unit
{
    public int Id { get; set; }
    public string Name { get; set; } = null!; // Not null

    public ICollection<Item>? Items { get; set; } = new List<Item>();
}