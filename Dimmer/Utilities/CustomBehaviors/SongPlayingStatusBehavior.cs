namespace Dimmer_MAUI.Utilities.CustomBehaviors;
public partial class SongPlayingStatusBehavior : Behavior<Border>
{
    private Border _associatedBorder;
    protected override void OnAttachedTo(Border bindable)
    {
        base.OnAttachedTo(bindable);

        _associatedBorder = bindable;
        if (_associatedBorder != null)
        {
            bindable.BindingContextChanged += OnBindingContextChanged;
        }
    }
    protected override void OnDetachingFrom(Border bindable)
    {
        base.OnDetachingFrom(bindable);
   
        // Clean up event handlers to prevent memory leaks
        bindable.BindingContextChanged -= OnBindingContextChanged;

        if (_associatedBorder.BindingContext is HomePageVM viewModel)
        {
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }        
    }

    private void OnBindingContextChanged(object? sender, EventArgs e)
    {
        if (_associatedBorder.BindingContext is HomePageVM viewModel)
        {
            // Detach previous event if any
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;

            // Attach to the new BindingContext
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        
        if (sender is HomePageVM viewModel)
        {
            var song= viewModel.TemporarilyPickedSong;
            if(song is not null)
            {
                if (e.PropertyName == nameof(song.IsCurrentPlayingHighlight))
                {
                    // Update the BackgroundColor based on IsPlaying            
                    _associatedBorder.Stroke = song.IsPlaying ? Colors.DarkSlateBlue : Colors.Transparent;
                }

            }
        }
    }
}