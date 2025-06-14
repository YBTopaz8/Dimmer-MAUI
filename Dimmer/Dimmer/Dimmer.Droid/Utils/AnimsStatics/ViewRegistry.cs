using View = Android.Views.View;

namespace Dimmer.Utils.AnimsStatics;
public class ViewRegistry
{

    public static Dictionary<string, View> AndroidViews = new();

    public static void Register(string name, View view)
    {
        AndroidViews[name] = view;
    }
    public static View? Get(string name)
    {
        return AndroidViews.TryGetValue(name, out var v) ? v : null;
    }
}
