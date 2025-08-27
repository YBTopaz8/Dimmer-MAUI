using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch.TQL.TQLCommands.Interfaces;
public interface ICommandAction { }

public record SavePlaylistAction(string Name, IReadOnlyList<SongModelView> Songs) : ICommandAction;
public record AddToNextAction(IReadOnlyList<SongModelView> Songs) : ICommandAction;
public record AddToEndAction(IReadOnlyList<SongModelView> Songs) : ICommandAction;
public record DeleteAllAction(IReadOnlyList<SongModelView> Songs) : ICommandAction;
public record DeleteDuplicateAction(IReadOnlyList<SongModelView> Songs) : ICommandAction;
public record UnrecognizedCommandAction(string CommandName) : ICommandAction;
public record NoAction : ICommandAction; // Represents a command that did nothing (e.g., save with no name)
public record ReplaceQueueAction(IReadOnlyList<SongModelView> Songs) : ICommandAction;
public record AddToPositionAction(int Position, IReadOnlyList<SongModelView> Songs) : ICommandAction;

// Represents > viewal 2
public record ViewAlbumAction(int AlbumIndex) : ICommandAction;

// Represents > scrollto
public record ScrollToPlayingAction : ICommandAction;

public record AddIndexedToQueueAction(HashSet<int> Indices, string Position, IReadOnlyList<SongModelView> SearchResults) : ICommandAction;