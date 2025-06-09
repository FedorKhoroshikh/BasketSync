namespace Domain.Entities;

public class Category
{
    public int Id { get; private set; }
    public string Name { get; private set; } = null!;

    private readonly List<Item>? _items = [];
    public IReadOnlyCollection<Item> Items => _items;

    public Category() { }
    public Category(string name) => Name = name;
}