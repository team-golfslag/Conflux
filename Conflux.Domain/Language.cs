// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Domain;

[Owned]
public class Language : IEquatable<Language>
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

    public bool Equals(Language? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Language)obj);
    }

    public override int GetHashCode() => Id.GetHashCode();
}
