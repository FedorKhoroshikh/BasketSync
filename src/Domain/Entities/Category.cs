namespace Domain.Entities;

public class Category
{
    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Comment { get; private set; }

    private readonly List<Item>? _items = [];
    public IReadOnlyCollection<Item> Items => _items;

    public Category() { }
    public Category(string name, string? comment = null)
    {
        Name = name;
        Comment = comment;
    }

    public void Update(string name, string? comment)
    {
        Name = name;
        Comment = comment;
    }
}