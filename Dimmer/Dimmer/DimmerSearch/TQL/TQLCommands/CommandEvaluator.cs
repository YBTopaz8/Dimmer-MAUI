using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch.TQL.TQLCommands;
public class CommandEvaluator
{
    public void Execute(IQueryNode node, IEnumerable<SongModelView>? currentResultsSet=null)
    {
        if (node is CommandNode cmdNode)
        {
            switch (cmdNode.Command.ToLowerInvariant())
            {
                case "save":
                    Debug.WriteLine(currentResultsSet is null);
                    SavePlaylist(cmdNode.Arguments["playlistName"].ToString());
                    break;
                case "addtoqueue":
                    AddToQueue((int)cmdNode.Arguments["id"]);
                    break;
                case "delete":
                    DeleteSong((int)cmdNode.Arguments["id"]);
                    break;
            }
        }
    }

    private void SavePlaylist(string? name) { Debug.WriteLine("save!"); }
    private void AddToQueue(int id) { Debug.WriteLine("Add to Q!"); }
    private void DeleteSong(int id) { Debug.WriteLine("delete!"); }
}
