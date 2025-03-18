using System.ComponentModel.DataAnnotations;

namespace Conflux.Domain;

/// <summary>
/// Represents a project.
/// </summary>
public class Project
{
    [Key] public Guid Id { get; set; }

    [Required] public required string Title { get; set; }

    public string? Description { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public List<Person> People { get; set; } = [];

    public List<Product> Products { get; set; } = [];

    public List<Party> Parties { get; set; } = [];
}
