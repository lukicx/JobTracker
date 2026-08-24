namespace JobTracker.Api.Models;

public class ExternalJob
{
    public string ExternalId { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Url { get; set; }
    public string? Description { get; set; }
}