using System;
using System.ComponentModel.DataAnnotations;

namespace Conflux.Domain;

public class Party
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; }
}
