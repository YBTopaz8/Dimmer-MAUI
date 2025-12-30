namespace Dimmer.Interfaces.IDatabase;
public  interface IDialogueService
{
    Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel);

    Task ShowAlertAsync(string title, string message, string accept);
    
}

public class DialogueService : IDialogueService
{
    public async Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel)
    {
        // DisplayAlert returns true if the user taps the accept button, false if they tap cancel
        return await Shell.Current.DisplayAlert(title, message, accept, cancel);
    }
    public async Task ShowAlertAsync(string title, string message, string accept)
    {
        await Shell.Current.DisplayAlert(title, message, accept);
    }
}