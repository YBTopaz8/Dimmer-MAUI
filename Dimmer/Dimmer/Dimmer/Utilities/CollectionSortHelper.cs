namespace Dimmer.Utilities;
public enum SortOrder
{
    Ascending,
    Descending
}

public static class CollectionSortHelper
{
    // Generic sorter for ObservableCollection
    public static ObservableCollection<SongModelView> Sort<SongModelView, TKey>(
        ObservableCollection<SongModelView> collection,
        Func<SongModelView, TKey> keySelector,
        SortOrder order = SortOrder.Ascending) where TKey : IComparable
    {
        if (collection == null || !collection.Any())
            return Enumerable.Empty<SongModelView>().ToObservableCollection();


        if (order == SortOrder.Ascending)
        {
            return collection.DistinctBy(keySelector).OrderBy(keySelector).ToObservableCollection();
        }
        else
        {
            return collection.DistinctBy(keySelector).OrderByDescending(keySelector).ToObservableCollection();
        }

        // Efficiently update the ObservableCollection without clearing and re-adding one by one
        // which can be slow and cause lots of UI updates.
        // This approach tries to move items to their correct sorted positions.
        //for (int i = 0; i < sortedObservableCollection.Count; i++)
        //{
        //    var itemToMove = sortedObservableCollection[i];
        //    int oldIndex = collection.IndexOf(itemToMove); // Relies on SongModelView.Equals being well-defined (you have it based on Id)

        //    if (oldIndex != i)
        //    {
        //        // Check if the item is still in the collection (could have been removed by another thread, though unlikely for UI collections)
        //        if (oldIndex >= 0)
        //        {
        //            collection.Move(oldIndex, i);
        //        }
        //        else
        //        {
        //            // Item wasn't found, might indicate an issue or concurrent modification.
        //            // For simplicity, we'll assume it should be there.
        //            // A more robust way if items can disappear is to rebuild:
        //            collection.Clear();
        //            foreach (var item in sortedObservableCollection)
        //                collection.Add(item);
        //            return;
        //        }
        //    }
        //}
    }

    // Specific sorters for SongModelView (convenience methods)
    public static ObservableCollection<SongModelView> SortByTitle(ObservableCollection<SongModelView> songs, SortOrder order = SortOrder.Ascending)
    {
        return Sort(songs, song => song.Title ?? string.Empty, order);
    }

    public static ObservableCollection<SongModelView> SortByArtistName(ObservableCollection<SongModelView> songs, SortOrder order = SortOrder.Ascending)
    {
        return Sort(songs, song => song.ArtistName ?? string.Empty, order);
    }

    public static ObservableCollection<SongModelView> SortByAlbumName(ObservableCollection<SongModelView> songs, SortOrder order = SortOrder.Ascending)
    {
        return Sort(songs, song => song.AlbumName ?? string.Empty, order);
    }

    public static ObservableCollection<SongModelView> SortByGenre(ObservableCollection<SongModelView> songs, SortOrder order = SortOrder.Ascending)
    {
        // Assuming GenreModelView has a Name property and is comparable
        // If Genre can be null, handle that.
        return Sort(songs, song => song.Genre?.Name ?? string.Empty, order);
    }

    public static ObservableCollection<SongModelView> SortByDuration(ObservableCollection<SongModelView> songs, SortOrder order = SortOrder.Ascending)
    {
        return Sort(songs, song => song.DurationInSeconds, order);
    }

    public static ObservableCollection<SongModelView> SortByReleaseYear(ObservableCollection<SongModelView> songs, SortOrder order = SortOrder.Ascending)
    {
        // Handle null ReleaseYear by sorting them to the beginning or end.
        // Here, nulls will typically sort before non-nulls in ascending.
        return Sort(songs, song => song.ReleaseYear ?? int.MinValue, order); // Or int.MaxValue for descending to put them last
    }

    public static ObservableCollection<SongModelView> SortByDateAdded(ObservableCollection<SongModelView> songs, SortOrder order = SortOrder.Ascending)
    {
        // Handle null DateCreated similarly
        return Sort(songs, song => song.DateCreated ?? DateTimeOffset.MinValue, order);
    }
}