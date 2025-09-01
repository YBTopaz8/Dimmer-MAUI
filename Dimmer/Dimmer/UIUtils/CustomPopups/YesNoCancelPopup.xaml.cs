using Syncfusion.Maui.Toolkit.Popup;

namespace Dimmer.UIUtils.CustomPopups;

public partial class YesNoCancelPopup : SfPopup
{
	public YesNoCancelPopup(BaseViewModel vm)
	{
		InitializeComponent();
		this.BindingContext = vm;
    }

	public string? PopUpHeaderText { get; set; } = "Confirm Action";
	public string? PopUpMessageText { get; set; } = "Are you sure you want to proceed?";
	public string? YesButtonText { get; set; } = "Yes";
	public string? NoButtonText { get; set; } = "No";	
	public string? CancelButtonText { get; set; } = "Cancel";
	public bool? IsYesButtonVisible { get; set; } = true;
	public bool? IsNoButtonVisible { get; set; } = true;
	public bool? IsCancelButtonVisible { get; set; } = true;
	public string? CustomMessageText { get; set; } = string.Empty;
	public void ShowPopup(string message, string header = "Confirm Action", bool isYesVisible = true, bool isNoVisible = true, bool isCancelVisible = true, string yesText = "Yes", string noText = "No", string cancelText = "Cancel", string customMessage = "")
	{
		PopUpHeaderText = header;
		PopUpMessageText = message;
		IsYesButtonVisible = isYesVisible;
		IsNoButtonVisible = isNoVisible;
		IsCancelButtonVisible = isCancelVisible;
		YesButtonText = yesText;
		NoButtonText = noText;
		CancelButtonText = cancelText;
		CustomMessageText = customMessage;
		this.Show();
    }
	object returnObject;
	public object? ClosePopup()
	{

		this.Dismiss();


		return returnObject;
	}
}