using System.ComponentModel.DataAnnotations;

namespace Conflux.Domain;

/// <summary>
/// Represents a project.
/// </summary>
public class Project
{
    [Key] public Guid Id { get; set; }

    [Required] [MaxLength(100)] public required string Title { get; set; }

    public string? Description { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public List<Person> People { get; set; } = [];

    public List<Product> Products { get; set; } = [];

    public List<Party> Parties { get; set; } = [];
}
