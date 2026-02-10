namespace Domain.Entities;

public class Item
{
    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    
    public Category Category { get; private set; } = null!;
    public int CategoryId { get; private set; }
    
    public Unit Unit { get; private set; } = null!;
    public int UnitId { get; private set; }
    
    private readonly List<ListItem> _listListItems = [];
    public IReadOnlyCollection<ListItem> ListItems => _listListItems;

    public Item() { }
    public Item(string name, Category category, Unit unit)
    {
        Name = name;
        Category = category;
        Unit = unit;
    }

    public void SetCategory(Category category)
    {
        Category = category;
        CategoryId = category.Id;
    }

    public void SetUnit(Unit unit)
    {
        Unit = unit;
        UnitId = unit.Id;
    }
}