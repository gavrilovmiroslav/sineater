namespace SINEATER;

public class Inventory
{
    private Item?[] _items = [ null, null, null, null, null, null, null, null, null, null, null, null ];
    public Item?[] Items => _items;

    public (bool, int) Put(Item source)
    {
        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i] == null)
            {
                _items[i] = source;
                return (true, i);
            }
        }

        return (false, -1);
    }

    public void Drop(int index)
    {
        if (_items[index] == null)
            return;
        
        _items[index] = null;
    }
}