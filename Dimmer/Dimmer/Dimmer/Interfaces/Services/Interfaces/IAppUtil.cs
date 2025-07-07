using Dimmer.DimmerSearch;
using Dimmer.DimmerSearch.AbstractQueryTree;

namespace Dimmer.Interfaces.Services.Interfaces;
public interface IAppUtil
{
    public Shell GetShell();
    public Window LoadWindow();
}
public interface IViewCriteria
{
    Func<SongModelView, bool> Filter { get; }
    IComparer<SongModelView> Comparer { get; }
    LimiterClause? Limiter { get; }
}
public class QueryBasedSongCriteria : IViewCriteria
{
    public Func<SongModelView, bool> Filter { get; }
    public IComparer<SongModelView> Comparer { get; }
    public LimiterClause? Limiter { get; }
    public string Query { get; }

    public QueryBasedSongCriteria(string query)
    {
        Query = query;

        var metaParser = new MetaParser(query);

        Filter = metaParser.CreateMasterPredicate();
        Comparer = metaParser.CreateSortComparer();
        Limiter = metaParser.CreateLimiterClause();
    }
}