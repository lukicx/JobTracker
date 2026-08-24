namespace JobTracker.Api.Dtos;

public class JoobleSearchRequest
{
    public string Keywords { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
}