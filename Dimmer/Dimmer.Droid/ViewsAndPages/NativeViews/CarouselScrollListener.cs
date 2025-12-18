
using Google.Android.Material.Carousel;

namespace Dimmer.ViewsAndPages.NativeViews;

internal class CarouselScrollListener : RecyclerView.OnScrollListener
{
    private CarouselLayoutManager _lm;
    private BaseViewModelAnd _vm;

    public CarouselScrollListener(CarouselLayoutManager layoutMgr, BaseViewModelAnd myViewModel)
    {
        this._lm = layoutMgr;
        this._vm = myViewModel;
    }
    public override void OnScrollStateChanged(RecyclerView recyclerView, int newState)
    {
        base.OnScrollStateChanged(recyclerView, newState);

        // When the user stops scrolling (IDLE)
        if (newState == RecyclerView.ScrollStateIdle)
        {
            // Find the center view (the one snapped)
            var centerView = new CarouselSnapHelper().FindSnapView(_lm);
            if (centerView != null)
            {
                int position = _lm.GetPosition(centerView);

                Toast.MakeText(recyclerView.Context, $"Centered on position: {position}", ToastLength.Short).Show();
                // Tell ViewModel to play this song
                //if (_vm.CurrentSongIndex.Value != position)
                //{
                //    _vm.SkipToCommand.Execute(position);
                //}
            }
        }
    }
}