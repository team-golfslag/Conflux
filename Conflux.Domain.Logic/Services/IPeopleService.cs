// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.DTOs.Requests;

namespace Conflux.Domain.Logic.Services;

public interface IPeopleService
{
    Task<List<Person>> GetPersonsByQueryAsync(string? query);
    Task<Person> GetPersonByIdAsync(Guid id);
    Task<Person?> GetPersonByOrcidIdAsync(string orcidId);
    Task<Person> CreatePersonAsync(PersonRequestDTO personDTO);
    Task<Person> UpdatePersonAsync(Guid id, PersonRequestDTO personDTO);
    Task DeletePersonAsync(Guid id);
}
