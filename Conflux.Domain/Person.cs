using System;
using System.ComponentModel.DataAnnotations;

namespace Conflux.Domain;

public class Person
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [Required]
    [Range(0, 150)]
    public int Age { get; set; }
}
