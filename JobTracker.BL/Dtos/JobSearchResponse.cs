using JobTracker.DAL.Models;

namespace JobTracker.BL.Dtos;

public class JobSearchResponse
{
    public List<ExternalJob> Jobs { get; set; } = [];
    public int? NextJobsCzPage { get; set; }
    public int? NextJooblePage { get; set; }
}
