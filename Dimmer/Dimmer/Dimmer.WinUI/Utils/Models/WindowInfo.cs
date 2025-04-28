namespace Dimmer.WinUI.Utils.Models
{
    public class WindowInfo
    {
        public Window WindowInstance { get; }
        public string? Title { get; }
        public string TypeName { get; }
        public ImageSource Thumbnail { get; }
        public double Width => WindowInstance.Width;
        public double Height => WindowInstance.Height;
        public double X => WindowInstance.X;
        public double Y => WindowInstance.Y;

        public WindowInfo(Window window, ImageSource thumbnail)
        {
            WindowInstance = window ?? throw new ArgumentNullException(nameof(window));
            Thumbnail      = thumbnail ?? throw new ArgumentNullException(nameof(thumbnail));
            Title          = window.Title;
            TypeName       = window.GetType().Name;
        }
    }
}
