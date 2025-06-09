namespace Domain.Entities;

public class ShoppingList
{
    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    
    public int UserId { get; private set; }
    public User User { get; private set; } = null!;
    
    private readonly List<ListItem> _listItems = [];
    public List<ListItem> ListItems => _listItems;
    
    public ShoppingList() { }
    public ShoppingList(string name, User user)
        => (Name, User) = (name, user);
    
    public ListItem AddItem(Item item, int quality)
    {
        var li = new ListItem(this, item, quality);
        _listItems.Add(li);
        return li;
    }

    public void RemoveItem(int listItemId)
    {
        var listItem = ListItems.SingleOrDefault(x => x.Id == listItemId);
        if (listItem is null)
            throw new KeyNotFoundException("List item not found");

        ListItems.Remove(listItem);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));
        
        Name = name;
    }
}