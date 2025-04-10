// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Conflux.Domain;

public static class ISessionExtensions
{
    public static void Set<T>(this ISession session, string key, T value) => 
        session.Set(key, JsonSerializer.SerializeToUtf8Bytes(value));

    public static T? Get<T>(this ISession session, string key) => 
        !session.TryGetValue(key, out byte[]? json) ? default : JsonSerializer.Deserialize<T>(json);
}
