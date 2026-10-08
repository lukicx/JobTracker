namespace JobTracker.DAL.Dtos;

public class JoobleSearchResponse
{
    public int TotalCount { get; set; }

    public List<JoobleJobDto> Jobs { get; set; } = [];
}

public class JoobleJobDto
{
    public long Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string? Snippet { get; set; }

    public string? Salary { get; set; }

    public string? Source { get; set; }

    public string? Type { get; set; }

    public string Link { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public DateTime? Updated { get; set; }
}