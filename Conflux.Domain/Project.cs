using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conflux.Domain;

/// <summary>
/// Represents a project.
/// </summary>
public class Project
{
    [Key]
    public Guid Id { get; set; }

    public string Title { get; set; }

    [Column(TypeName = "text")]
    public string? Description { get; set; }

    public List<Person> People { get; set; } = [];

    public List<Product> Products { get; set; } = [];

    public Party Party { get; set; }
}
