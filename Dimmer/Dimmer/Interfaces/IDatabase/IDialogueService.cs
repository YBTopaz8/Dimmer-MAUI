using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        await Shell.Current.DisplayAlert(title, message, accept, cancel);
        // Implementation of the confirmation dialog
        // This is a placeholder; actual implementation will depend on the UI framework being used.
        return await Task.FromResult(true); // Simulate user accepting the dialog
    }
    public async Task ShowAlertAsync(string title, string message, string accept)
    {
        await Shell.Current.DisplayAlert(title, message, accept);
        // Implementation of the alert dialog
        // This is a placeholder; actual implementation will depend on the UI framework being used.
        await Task.CompletedTask; // Simulate showing the alert
    }
}