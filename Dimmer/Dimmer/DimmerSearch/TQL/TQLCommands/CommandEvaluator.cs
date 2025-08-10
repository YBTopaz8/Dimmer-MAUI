using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch.TQL.TQLCommands;


public class CommandEvaluator
{
    // Define constants for command names and argument keys to avoid magic strings.
    public static class CommandKeys
    {
        public const string Save = "save";
        public const string AddNext = "addnext";
        public const string AddEnd = "addend";
        public const string PlaylistNameArg = "playlistName";
        public const string DeleteAll = "deleteall";
        public const string DeleteDuplicate = "deletedup";
    }

    // --- Subjects for publishing events ---
    private readonly Subject<(string Name, IEnumerable<SongModelView> Songs)> _savePlaylistSubject = new();
    private readonly Subject<IEnumerable<SongModelView>> _addToNextSubject = new();
    private readonly Subject<IEnumerable<SongModelView>> _addToEndSubject = new();
    private readonly Subject<IEnumerable<SongModelView>> _deleteAllSubject = new();
    private readonly Subject<IEnumerable<SongModelView>> _deleteDuplicateSubject = new();
    // --- Public-facing observables for subscribers ---
    public IObservable<(string Name, IEnumerable<SongModelView> Songs)> SavePlaylistRequested => _savePlaylistSubject.AsObservable();
    public IObservable<IEnumerable<SongModelView>> AddToNextRequested => _addToNextSubject.AsObservable();
    public IObservable<IEnumerable<SongModelView>> AddToEndRequested => _addToEndSubject.AsObservable();
    public IObservable<IEnumerable<SongModelView>> DeleteAllRequested => _deleteAllSubject.AsObservable();
    public IObservable<IEnumerable<SongModelView>> DeleteDuplicateRequested => _deleteDuplicateSubject.AsObservable();

    public void Execute(IQueryNode node, IEnumerable<SongModelView>? currentResultsSet)
    {

        if (node is not CommandNode cmdNode || currentResultsSet == null || !currentResultsSet.Any())
        {
            return;
        }

        switch (cmdNode.Command.ToLowerInvariant())
        {
            case CommandKeys.Save:
                // Safely get the playlist name from the arguments.
                if (cmdNode.Arguments.TryGetValue(CommandKeys.PlaylistNameArg, out object? value) &&
                    value is string playlistName &&
                    !string.IsNullOrWhiteSpace(playlistName))
                {
                    _savePlaylistSubject.OnNext((playlistName, currentResultsSet));
                }

                break;

            case CommandKeys.AddNext:
                _addToNextSubject.OnNext(currentResultsSet);
                break;

            case CommandKeys.AddEnd:
                _addToEndSubject.OnNext(currentResultsSet);
                break;
            case CommandKeys.DeleteAll:
                _deleteAllSubject.OnNext(currentResultsSet);
                break;
            case CommandKeys.DeleteDuplicate:
                _deleteDuplicateSubject.OnNext(currentResultsSet);
                break;
                

        }
    }
}