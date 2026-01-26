
namespace Dimmer.Data.Models;

public enum DimmerLogLevel
{
    Info,
    Success,
    Warning,
    Error,
    Progress
}


public class AppLogEntry : RealmObject, IRealmObjectWithObjectId
{
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

    public string Category { get; set; } = string.Empty;

    public string Operation { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string LevelStr { get; set; } = DimmerLogLevel.Info.ToString();

    public int? ProgressValue { get; set; }
    public int? ProgressTotal { get; set; }

    public string ContextData { get; set; } = string.Empty;

    public string? ExceptionTrace { get; set; } // For Errors
    public string CorrelationId { get; set; } = string.Empty; // To group async steps together
    [PrimaryKey]
    [MapTo("_id")]
    public ObjectId Id { get; set; }
}
public class AppScanLogModel
{
    public int TotalFiles { get; set; }
    public int CurrentFilePosition { get; set; }
    
}