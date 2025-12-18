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

    // The method is renamed to better reflect its purpose and now returns an action.
    // It is now STATELESS. No subjects, no observables.
    public ICommandAction Evaluate(IQueryNode node, IEnumerable<SongModelView>? currentResultsSet)
    {
        var resultsSnapshot = currentResultsSet?.ToList() ?? new List<SongModelView>();
        if (node is not CommandNode cmdNode)
        {
            if (resultsSnapshot.Count==0)
            {
                return new ReplaceQueueAction(resultsSnapshot);
            }
            return new NoAction();
        }


        if (resultsSnapshot.Count==0 && !cmdNode.Command.Equals(CommandKeys.Save, StringComparison.InvariantCultureIgnoreCase)) // Allow saving an empty playlist
        {
            return new NoAction(); // Don't execute most commands on empty result sets
        }

        switch (cmdNode.Command.ToLowerInvariant())
        {
            case CommandKeys.Save:
                if (cmdNode.Arguments.TryGetValue(CommandKeys.PlaylistNameArg, out object? value) &&
                    value is string playlistName &&
                    !string.IsNullOrWhiteSpace(playlistName))
                {
                    return new SavePlaylistAction(playlistName, resultsSnapshot);
                }
                return new NoAction(); // Or a specific error action

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
                {
                    // We subtract 1 because queues are 0-indexed, but users think 1-indexed.
                    return new AddToPositionAction(Math.Max(0, position - 1), resultsSnapshot);
                }
                return new NoAction(); // Or an error action

            case "viewal":
                if (cmdNode.Arguments.TryGetValue("albumIndex", out object? idxValued) && idxValued is int albumIndex)
                {
                    return new ViewAlbumAction(Math.Max(0, albumIndex - 1));
                }
                return new NoAction();

            case "scrollto":
                return new ScrollToPlayingAction();

            default:
                return new UnrecognizedCommandAction(cmdNode.Command);
        }
    }
}