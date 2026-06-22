namespace Dimmer.Hoarder.Models;

public class OrganizationResult
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Logs { get; set; } = new();
}