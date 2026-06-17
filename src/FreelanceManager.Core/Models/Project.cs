namespace FreelanceManager.Core.Models;

public class Project
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client? Client { get; set; }

    public string Title { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; } = ProjectStatus.Lead;

    public string? RepoUrl { get; set; }
    public string? LiveSiteUrl { get; set; }
    public string? HostingNotes { get; set; }
    public string? CredentialsLocation { get; set; }
    public string? BuildStackNotes { get; set; }
    public string? GeneralNotes { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Invoice> Invoices { get; set; } = new();
}
