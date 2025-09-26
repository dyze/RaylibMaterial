namespace Editor.Helpers;

public static class CollectionHelpers
{
    /// <summary>
    /// Add an entry to a list without exceeding its capacity, last added entries are on the top a 
    /// </summary>
    /// <param name="list"></param>
    /// <param name="entry"></param>
    /// <param name="maxListSize"></param>
    public static void AddEntryToHistory(List<string> list, string entry, int maxListSize)
    {
        var index = list.FindIndex(f => f == entry);
        if (index >= 0)
        {
            list.RemoveAt(index);
            list.Insert(0, entry);
            return;
        }

        if (list.Count >= maxListSize)
        {
            var startIndex = maxListSize - 1;
            var count = list.Count - startIndex;
            list.RemoveRange(startIndex, count);
        }
        list.Insert(0, entry);
    }
}