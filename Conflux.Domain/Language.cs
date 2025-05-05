// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Domain;

[Owned]

public class Language
{
    [MaxLength(3)] public string Id { get; init; }

    [JsonIgnore] public string SchemaUri => "https://www.iso.org/standard/74575.html";
    
    public static Language ENGLISH =>
        new()
        {
            Id = "eng",
        };

    public static Language DUTCH =>
        new()
        {
            Id = "nld",
        };
}
