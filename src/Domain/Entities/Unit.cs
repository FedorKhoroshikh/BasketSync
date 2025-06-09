namespace Domain.Entities;

public class Unit
{
    public int Id { get; private set; }
    public string Name { get; private set; } = null!;

    private readonly List<Item> _items = [];
    public IReadOnlyCollection<Item> Items => _items;

    private Unit() { }
    public Unit(string name) => Name = name;
}