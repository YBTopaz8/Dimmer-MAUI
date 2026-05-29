global using DXSortDescription = DevExpress.Maui.CollectionView.SortDescription;
using DevExpress.Maui.CollectionView;


namespace Dimmer.Utils.Utils.Extensions;

public static class CollectionViewSortExtensions
{
    public static void ApplySortPreset(this DXCollectionView collectionView, string preset)
    {
        collectionView.SortDescriptions.Clear();

        switch (preset)
        {
            case "Title (A-Z)":
                collectionView.SortDescriptions.Add(new DevExpress.Maui.CollectionView.SortDescription
                { FieldName = "Title", SortOrder = DataSortOrder.Ascending });
                break;

            case "Title (Z-A)":
                collectionView.SortDescriptions.Add(new DXSortDescription
                { FieldName = "Title", SortOrder = DataSortOrder.Descending });
                break;

            case "Artist (A-Z)":
                collectionView.SortDescriptions.Add(new DXSortDescription
                { FieldName = "ArtistName", SortOrder = DataSortOrder.Ascending });
                break;

            case "Artist (Z-A)":
                collectionView.SortDescriptions.Add(new DXSortDescription
                { FieldName = "ArtistName", SortOrder = DataSortOrder.Descending });
                break;

            case "Most Played":
                collectionView.SortDescriptions.Add(new DXSortDescription
                { FieldName = "PlayCount", SortOrder = DataSortOrder.Descending });
                break;

            case "Least Played":
                collectionView.SortDescriptions.Add(new DXSortDescription
                { FieldName = "PlayCount", SortOrder = DataSortOrder.Ascending });
                break;

            case "Top Rated":
                collectionView.SortDescriptions.Add(new DXSortDescription
                { FieldName = "Rating", SortOrder = DataSortOrder.Descending });
                collectionView.SortDescriptions.Add(new DXSortDescription
                { FieldName = "PlayCount", SortOrder = DataSortOrder.Descending });
                break;

            case "Recently Added":
                collectionView.SortDescriptions.Add(new DXSortDescription
                { FieldName = "DateCreated", SortOrder = DataSortOrder.Descending });
                break;

            case "Recently Played":
                collectionView.SortDescriptions.Add(new DXSortDescription
                { FieldName = "LastPlayed", SortOrder = DataSortOrder.Descending });
                break;

            case "Longest Duration":
                collectionView.SortDescriptions.Add(new DXSortDescription
                { FieldName = "DurationInSeconds", SortOrder = DataSortOrder.Descending });
                break;

            case "Shortest Duration":
                collectionView.SortDescriptions.Add(new DXSortDescription
                { FieldName = "DurationInSeconds", SortOrder = DataSortOrder.Ascending });
                break;

            case "Most Favorited":
                collectionView.SortDescriptions.Add(new DXSortDescription
                { FieldName = "NumberOfTimesFaved", SortOrder = DataSortOrder.Descending });
                break;
        }
    }
}
