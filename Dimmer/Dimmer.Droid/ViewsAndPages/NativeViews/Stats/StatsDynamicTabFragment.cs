using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Widget;
using Dimmer.ViewModel;
using DynamicData.Binding;

namespace Dimmer.ViewsAndPages.NativeViews.Stats;

public class StatsDynamicTabFragment : Fragment
{
    private readonly StatisticsViewModel _viewModel;

    // Functions injected via constructor to pull the specific data we want
    private readonly Func<StatisticsViewModel, Dictionary<string, string>?> _gridExtractor;
    private readonly Func<StatisticsViewModel, Dictionary<string, List<DimmerStats>>?>? _carouselExtractor;

    private CompositeDisposable _disposables = new();
    private LinearLayout _rootLayout = null!;

    public StatsDynamicTabFragment(
        StatisticsViewModel viewModel,
        Func<StatisticsViewModel, Dictionary<string, string>?> gridExtractor,
        Func<StatisticsViewModel, Dictionary<string, List<DimmerStats>>?>? carouselExtractor)
    {
        _viewModel = viewModel;
        _gridExtractor = gridExtractor;
        _carouselExtractor = carouselExtractor;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = RequireContext();
        var scroll = new NestedScrollView(ctx) { FillViewport = true };
        _rootLayout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        _rootLayout.SetPadding(0, 20, 0, 160);
        scroll.AddView(_rootLayout);
        return scroll;
    }

    public override void OnResume()
    {
        base.OnResume();
        _disposables = new CompositeDisposable();

        // Listen to the ViewModel. When ANY stat bundle updates, this re-renders.
        _viewModel.WhenValueChanged(x => x.IsBusy)
            .Where(isBusy => !isBusy) // Only update when it finishes loading
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(_ => RenderUI())
            .DisposeWith(_disposables);

        RenderUI(); // Initial render
    }

    private void RenderUI()
    {
        _rootLayout.RemoveAllViews();

        // 1. Render Metrics Grid (if provided)
        var gridData = _gridExtractor?.Invoke(_viewModel);
        if (gridData != null && gridData.Count > 0)
        {
            _rootLayout.AddView(StatsUIEngine.BuildMetricsGrid(RequireContext(), gridData));
        }

        // 2. Render Carousels (if provided)
        var carouselData = _carouselExtractor?.Invoke(_viewModel);
        if (carouselData != null)
        {
            foreach (var carousel in carouselData)
            {
                if (carousel.Value != null && carousel.Value.Count > 0)
                {
                    _rootLayout.AddView(StatsUIEngine.BuildHorizontalCarousel(RequireContext(), carousel.Key, carousel.Value));
                }
            }
        }
    }

    public override void OnPause()
    {
        base.OnPause();
        _disposables.Dispose();
    }
}