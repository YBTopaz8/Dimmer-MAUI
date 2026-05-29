using DevExpress.Maui.CollectionView;

namespace Dimmer.Views.CustomViews;

public partial class FilterSortPopup : DXPopup
{
	public FilterSortPopup()
	{
		InitializeComponent();
    }
    private readonly FilterSortViewModel _viewModel;

    public FilterSortPopup(DXCollectionView collectionView, BaseViewModel mainViewModel)
    {
        InitializeComponent();
        _viewModel = new FilterSortViewModel(collectionView, mainViewModel);
        BindingContext = _viewModel;
    }

    private async void OnApplyAndCloseClicked(object sender, EventArgs e)
    {
        this.Close();
    }

}