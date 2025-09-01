using View = Microsoft.Maui.Controls.View;

namespace Dimmer.Utils.AnimsStatics;
public static class Transition
{
   
    
}
public static class SharedTransitions
    {
        // This is the property we will use in XAML
        public static readonly BindableProperty TransitionNameProperty =
            BindableProperty.CreateAttached("TransitionName", typeof(string), typeof(SharedTransitions), null);

        public static string GetTransitionName(BindableObject view)
        {
            return (string)view.GetValue(TransitionNameProperty);
        }

        public static void SetTransitionName(BindableObject view, string value)
        {
            view.SetValue(TransitionNameProperty, value);
        }
    }