// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using SURFSharekit.Net.Models.RepoItem;

namespace Conflux.Domain.Logic.Services;

public interface ISURFSharekitService
{
    string HandleWebhook(SURFSharekitRepoItem payload);
    Task<List<string>> UpdateRepoItems();
    Task<SURFSharekitService.ProcessReturnValues?> ProcessRepoItem(SURFSharekitRepoItem payload);
}
