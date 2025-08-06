using View = Microsoft.Maui.Controls.View;

namespace Dimmer.Utils.AnimsStatics;
public static class Transition
{
    public static readonly BindableProperty NameProperty =
        BindableProperty.CreateAttached(
            "Name",
            typeof(string),
            typeof(Transition),
            null,
            propertyChanged: OnNameChanged);

    public static string GetName(BindableObject view) => (string)view.GetValue(NameProperty);
    public static void SetName(BindableObject view, string value) => view.SetValue(NameProperty, value);

    private static void OnNameChanged(BindableObject bindable, object oldValue, object newValue)
    {

        if (bindable is not View mauiView)
            return;

        // Wait for the handler and platform view to be available
        mauiView.HandlerChanged += (s, e) =>
        {
            if (mauiView.Handler?.PlatformView is Android.Views.View nativeView)
            {
                // This is the magic link to the native Android transition system!
                nativeView.TransitionName = newValue as string;
            }
        };

        // Apply immediately if handler already exists
        if (mauiView.Handler?.PlatformView is Android.Views.View nativeView)
        {
            nativeView.TransitionName = newValue as string;
        }

    }

}