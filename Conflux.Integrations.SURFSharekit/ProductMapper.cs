// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;
using Crossref.Net.Models;
using Crossref.Net.Services;
using DoiTools.Net;
using Microsoft.Extensions.Logging;
using SURFSharekit.Net.Models;
using SURFSharekit.Net.Models.RepoItem;

namespace Conflux.Integrations.SURFSharekit;

public class ProductMapper
{
    private readonly CrossrefService _crossrefClient;

    public ProductMapper(CrossrefService crossrefClient)
    {
        _crossrefClient = crossrefClient;
    }
    
    /// <summary>
    /// Given a list of SURFSharekit repo items,
    /// it maps each repo item contained within to a product.
    /// Invalid repo items are excluded.
    /// </summary>
    /// <param name="repoItems"></param>
    /// <returns>A list of products</returns>
    public async Task<List<Product>> MultipleRepoItemsToProducts(List<SURFSharekitRepoItem> repoItems)
    {
        var tasks = repoItems.Select(MapProduct).ToList();
        var result = await Task.WhenAll(tasks);
        return result.OfType<Product>().ToList();
    }

    /// <summary>
    /// Given a single SURFSharekit repo item,
    /// it maps it to a product. If the repo item is missing crucial data, like the title, this may return null.
    /// </summary>
    /// <param name="repoItem"></param>
    /// <returns>A product or null</returns>
    public async Task<Product?> SingleRepoItemToProduct(SURFSharekitRepoItem repoItem)
    {
        return await MapProduct(repoItem);
    }

    /// <summary>
    /// A helper function which takes a SURFSharekit repo item
    /// and converts it to a product
    /// </summary>
    /// <param name="repoItem"></param>
    /// <returns>A Product or null</returns>
    private async Task<Product?> MapProduct(SURFSharekitRepoItem repoItem)
    {
        if (string.IsNullOrWhiteSpace(repoItem.Attributes?.Title)) //product requires a title to be made
            return null;
        
        string title = repoItem.Attributes.Title;

        ProductType? productType = null;
        if (!string.IsNullOrWhiteSpace(repoItem.Attributes.Doi))
        {

            Doi? doi;
            Doi.TryParse(repoItem.Attributes.Doi, out doi);
            if (doi != null)
            {
                productType = await DoiToProductType(doi);
            }
        }
        
        Guid newId = Guid.NewGuid();
        Product mappedProduct = new()
        {
            Title = title,
            Id = newId,
            Type = productType,
            Url = repoItem.Attributes.Doi,
            Schema = ProductSchema.Doi,
            Categories =
            [
                new()
                {
                    ProductId = newId,
                    Type = ProductCategoryType.Input,
                },
            ],
            SURFSharekitId = repoItem.Id,
        };

        return mappedProduct;
    }
    
    /// <summary>
    /// Given a doi, this function attempt to use crossref in order to get the work associated with it.
    /// With that work, it then tries to map it to a specific product type. 
    /// </summary>
    /// <param name="doi"></param>
    /// <returns>null or a productType</returns>
    private async Task<ProductType?> DoiToProductType(Doi doi)
    {
        try
        {
            Work? work = await _crossrefClient.GetWork(doi); //Q: does this already have a timeout? Does it need it?
            if (work is null) return null;
            switch (work.Type)
            {
                case "book":
                    return ProductType.Book;
                
                case "book-chapter":
                    return ProductType.BookChapter;
                
                case "conference_paper":
                    return ProductType.ConferencePaper;
                                
                case "dataset":
                    return ProductType.Dataset;
                
                case "editor-report":
                    //unsure if a report has anything more specific
                    return ProductType.Text;
                
                case "journal_article":
                    return ProductType.JournalArticle; 
                
                case "preprint": 
                    return ProductType.Preprint;
                
                case "report" : 
                    return ProductType.Report;

                case "other":  //should other be something more specifc?
                default:
                    //type was either null or something not registered in the schema
                    return null;
            }
            
        }
        catch (Exception ex)
        {
            //in this case, we either timed out or the crossref client did not recognize the doi.
            return null;
        }
    }
}
