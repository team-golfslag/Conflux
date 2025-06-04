// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Integrations.SURFSharekit;
using Crossref.Net.Services;
using Microsoft.EntityFrameworkCore;
using RAiD.Net;
using SURFSharekit.Net;
using SURFSharekit.Net.Models.RepoItem;
using SURFSharekit.Net.Models.Webhooks;
using SURFSharekit.Net.Models.WebhooksByDocumentation;

using Conflux.Integrations.SURFSharekit;


namespace Conflux.Domain.Logic.Services;

public class SURFSharekitService : ISURFSharekitService
{
    private readonly ConfluxContext _context;
    private readonly IRAiDService _raidService;
    private readonly SURFSharekitApiClient _SURFSharekitApiClient;
    private readonly ProductMapper _productMapper;

    public SURFSharekitService(ConfluxContext context, IRAiDService raidService,
        SURFSharekitApiClient surfSharekitApiClient, CrossrefService crossrefService)
    {
        _context = context;
        _raidService = raidService;
        _SURFSharekitApiClient = surfSharekitApiClient;
        _productMapper = new(crossrefService);
    }

    // basewebhookDTO
    public string HandleWebhook(SURFSharekitRepoItem payload)
    {
        ProcessRepoItem(payload);

        return null;
    }

    public async Task<List<string>> UpdateRepoItems()
    {
        // get all repo items from SURFSharekit
        List<SURFSharekitRepoItem> result = await _SURFSharekitApiClient.GetAllRepoItems();
        if (result == null) throw new("SURFSharekit data is null");

        List<ProcessReturnValues> updatedItems = await ProcessRepoItemList(result);

        return updatedItems.Select(i => i.SURFSharekitId).ToList();
    }

    public struct ProcessReturnValues(string surfSharekitId, SURFSharekitWebhookPayloadType type)
    {
        public readonly string SURFSharekitId = surfSharekitId;
        public readonly SURFSharekitWebhookPayloadType Type = type;
    }

    public async Task<ProcessReturnValues?> ProcessRepoItem(SURFSharekitRepoItem payload)
    {
        // if there is no RAiD, the repo item will not get processed
        if (payload.Id is null || payload.Attributes?.Raid is null) return null;

        switch (payload)
        {
            // webhookcreate is the same type as a normal get request response, so the update function will also use the webhookcreate
            case SURFSharekitWebhookCreate webhookCreate:
                return await ProcessWebhookCreate(webhookCreate);
            // NOT YET IMPLEMENTED in SURFSharekit.Net
            // case SURFSharekitWebhookUpdate webhookUpdate:
            // return await ProcessWebhookUpdate(webhookUpdate);
            case SURFSharekitWebhookDelete webhookDelete:
                return await ProcessWebhookDelete(webhookDelete);
            default:
                throw new($"Unknown webhook type: {payload.GetType().Name}");
        }
    }

    private async Task<ProcessReturnValues?> ProcessWebhookCreate(SURFSharekitWebhookCreate webhookCreate)
    {
        if (webhookCreate.Attributes is null || webhookCreate.Id is null) return null;
        // map naar product
        Product? product = await _productMapper.SingleRepoItemToProduct(webhookCreate);
        if (product == null) return null;

        // via raid koppelen aan bijbehorend project
        
        // TODO: new project when raid does not match
        Project project = _context.Projects.Include(project => project.Organisations)
            .Include(project => project.Contributors).First(p =>
                p.RAiDInfo != null && p.RAiDInfo.RAiDId == webhookCreate.Attributes.Raid);

        project.Products.Add(product);
        _context.Products.Add(product);

        await FillOrganisation(webhookCreate.Attributes.Owner, project);
        await FillContributors(webhookCreate.Attributes.Authors, project);
        // owners naar organisations mappen
        // authors naar collaborators mappen (koppelen op orcid of email)
        // als geen orcid of email, maak nieuw persoon

        await _context.SaveChangesAsync();
        return new(webhookCreate.Id, SURFSharekitWebhookPayloadType.Create);
    }

    private async Task<ProcessReturnValues?> ProcessWebhookDelete(SURFSharekitWebhookDelete webhookDelete)
    {
        if (webhookDelete.Attributes is null || webhookDelete.Id is null) return null;

        Product product = _context.Products.First(p => p.SURFSharekitId == webhookDelete.Id);

        _context.Products.Remove(product);
        Project project = _context.Projects.First(p => p.Products.Contains(product));
        project.Products.Remove(product);
        await _context.SaveChangesAsync();

        return new(webhookDelete.Id, SURFSharekitWebhookPayloadType.Delete);
    }

    private async Task FillOrganisation(SURFSharekitOwner? owner, Project project)
    {
        if (owner is null) return;

        Organisation? organisation = OrganisationMapper.MapOrganisation(owner);
        if (organisation is not null)
        {
            Organisation? existingOrg = await
                _context.Organisations.FirstOrDefaultAsync(o => o.Name == organisation.Name);
            if (existingOrg is null) await _context.Organisations.AddAsync(organisation);
            else organisation = existingOrg;

            if (!project.Organisations.Contains(organisation)) project.Organisations.Add(organisation);
        }
    }

    private async Task FillContributors(List<SURFSharekitAuthor>? authors, Project project)
    {
        if (authors is null) return;

        foreach (SURFSharekitAuthor author in authors)
        {
            // bestaat al in database?
            Person? existingPerson = DoesPersonExist(author);
            if (existingPerson is null) continue;

            _context.People.Add(existingPerson);

            if (!IsAlreadyLinked(author, project))
            {
                Contributor contributor = new()
                {
                    PersonId = existingPerson.Id,
                    ProjectId = project.Id,
                };
                project.Contributors.Add(contributor);
            }

            await _context.SaveChangesAsync();
        }
    }

    private async Task<List<ProcessReturnValues>> ProcessRepoItemList(List<SURFSharekitRepoItem> items)
    {
        var results = new List<ProcessReturnValues>();
        foreach (SURFSharekitRepoItem item in items)
        {
            if (await ProcessRepoItem(item) is { } processReturnValues)
                results.Add(processReturnValues);
        }

        return results;
    }

    private bool IsAlreadyLinked(SURFSharekitAuthor author, Project project)
    {
        List<Contributor> contributorList = project.Contributors;
        foreach (Contributor contributor in contributorList)
        {
            if (_context.People.FirstOrDefault(p => p.Id == contributor.PersonId) is not { } person)
            {
                throw new("Contributor has no linked person");
            }

            // author is already contributor to the project
            if (person.ORCiD == author.Person?.Orcid) return true;
        }

        return false;
    }

    private Person? DoesPersonExist(SURFSharekitAuthor author)
    {
        Person? existingPerson = null;
        if (author.Person?.Orcid is not null)
        {
            existingPerson = _context.People.FirstOrDefault(p => p.ORCiD == author.Person.Orcid);
            existingPerson ??= _context.People.FirstOrDefault(p => p.Email == author.Person.Email);
        }

        if (existingPerson is null)
        {
            if (author.Person is null) return null;

            existingPerson = PersonMapper.MapPerson(author.Person);
        }

        return existingPerson;
    }
    
}
