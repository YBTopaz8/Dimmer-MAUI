using Android.Views.InputMethods;
using Window = Microsoft.Maui.Controls.Window;

namespace Dimmer.Utils;
internal class AppUtil : IAppUtil
{
   
    public static void HideKeyboardFrom(Context context, Android.Views.View view)
    {
        InputMethodManager? imm = (InputMethodManager?)context.GetSystemService(Activity.InputMethodService);
        imm?.HideSoftInputFromWindow(view.WindowToken, 0);
    }
   
    public static void ShowKeyboardTo(Context context, Android.Views.View view)
    {
        InputMethodManager? imm = (InputMethodManager?)context.GetSystemService(Activity.InputMethodService);
        imm?.ShowSoftInput(view, 0);
    }

    public AppUtil(BaseViewModelAnd vm)
    {
        baseViewModelAnd = vm;

    }
    BaseViewModelAnd baseViewModelAnd { get; }
    public Shell GetShell()
    {
        return new DimmerShell(baseViewModelAnd);
    }

    public Window LoadWindow()
    {
        Window window = new Window();
        window.Page = GetShell();
        return window;
    }

}
