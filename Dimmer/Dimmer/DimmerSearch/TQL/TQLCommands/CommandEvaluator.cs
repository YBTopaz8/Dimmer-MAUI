namespace Dimmer.DimmerSearch.TQL.TQLCommands;

public class CommandEvaluator
{
    public static class CommandKeys
    {
        public const string Play = "play";
        public const string Save = "save";
        public const string AddNext = "addnext";
        public const string AddEnd = "addend";
        public const string PlaylistNameArg = "playlistName";
        public const string DeleteAll = "deleteall";
        public const string DeleteDuplicate = "deletedup";
    }

    public ICommandAction Evaluate(IQueryNode node, IEnumerable<SongModelView>? currentResultsSet)
    {
        if (node is not CommandNode cmdNode)
            return new NoAction();

        var resultsSnapshot = currentResultsSet?.ToList() ?? new List<SongModelView>();

        if (resultsSnapshot.Count == 0 && !cmdNode.Command.Equals(CommandKeys.Save, StringComparison.InvariantCultureIgnoreCase))
            return new NoAction();

        switch (cmdNode.Command.ToLowerInvariant())
        {
            case CommandKeys.Save:
                if (cmdNode.Arguments.TryGetValue(CommandKeys.PlaylistNameArg, out object? value) &&
                    value is string playlistName &&
                    !string.IsNullOrWhiteSpace(playlistName))
                {
                    return new SavePlaylistAction(playlistName, resultsSnapshot);
                }
                return new NoAction();

            case CommandKeys.AddNext:
                return new AddToNextAction(resultsSnapshot);

            case CommandKeys.AddEnd:
                return new AddToEndAction(resultsSnapshot);

            case CommandKeys.DeleteAll:
                return new DeleteAllAction(resultsSnapshot);

            case CommandKeys.DeleteDuplicate:
                return new DeleteDuplicateAction(resultsSnapshot);

            case CommandKeys.Play:
                return new ReplaceQueueAction(resultsSnapshot);

            case "addall":
                if (cmdNode.Arguments.TryGetValue("indices", out object? idxValue) && idxValue is HashSet<int> indices &&
                    cmdNode.Arguments.TryGetValue("position", out object? posValued) && posValued is string positiond)
                {
                    return new AddIndexedToQueueAction(indices, positiond, resultsSnapshot);
                }
                return new NoAction();

            case "addto":
            case "addtopos":
                if (cmdNode.Arguments.TryGetValue("position", out object? posValue) && posValue is int position)
                    return new AddToPositionAction(Math.Max(0, position - 1), resultsSnapshot);
                return new NoAction();

            case "viewal":
                if (cmdNode.Arguments.TryGetValue("albumIndex", out object? idxValued) && idxValued is int albumIndex)
                    return new ViewAlbumAction(Math.Max(0, albumIndex - 1));
                return new NoAction();

            case "scrollto":
                return new ScrollToPlayingAction();

            default:
                return new UnrecognizedCommandAction(cmdNode.Command);
        }
    }
}