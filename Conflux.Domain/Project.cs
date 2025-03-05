using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Conflux.Domain;

public class Project
{
    [Key]
    public Guid Id { get; set; }
    public string Title { get; set; }

    // Many-to-many relationship with Person
    public List<Person> People { get; set; } = [];

    // Many-to-many relationship with Product
    public List<Product> Products { get; set; } = [];

    // Relationship to a Party (one-to-one or one-to-many depending on your design)
    public Party Party { get; set; }
}
