using Dimmer.DimmerSearch;
using Dimmer.DimmerSearch.AbstractQueryTree;

namespace Dimmer.Interfaces.Services.Interfaces;
public interface IAppUtil
{
    public Shell GetShell();
    public Window LoadWindow();
}