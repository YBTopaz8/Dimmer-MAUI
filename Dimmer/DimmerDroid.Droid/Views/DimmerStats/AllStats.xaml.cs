namespace Dimmer.Views.DimmerStats;

public partial class AllStats : ContentPage
{
    readonly StatisticsViewModel StatisticsViewModel;
    public AllStats(StatisticsViewModel statisticsViewModel)
	{
        this.StatisticsViewModel = statisticsViewModel;
        InitializeComponent();
		BindingContext = StatisticsViewModel;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StatisticsViewModel.LoadLibraryStatsCommand.Execute(null);
    }
}