// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;
using SURFSharekit.Net.Models;

namespace Conflux.Integrations.SURFSharekit;

public static class ProductMapper
{
    /// <summary>
    /// Given a list of SURFSharekit repo items,
    /// it maps each repo item contained within to a product.
    /// Invalid repo items are excluded.
    /// </summary>
    /// <param name="repoItems"></param>
    /// <returns>A list of products</returns>
    public static List<Product> MultipleRepoItemsToProducts(List<SURFSharekitRepoItem> repoItems)
    {
        return repoItems.Select(MapProduct).OfType<Product>().ToList();
    }

    /// <summary>
    /// Given a single SURFSharekit repo item,
    /// it maps it to a product. If the repo item is missing crucial data, like the title, this may return null.
    /// </summary>
    /// <param name="repoItem"></param>
    /// <returns>A product or null</returns>
    public static Product? SingleRepoItemToProduct(SURFSharekitRepoItem repoItem)
    {
        return MapProduct(repoItem);
    }

    /// <summary>
    /// A helper function which takes a SURFSharekit repo item
    /// and converts it to a product
    /// </summary>
    /// <param name="repoItem"></param>
    /// <returns>A Product or null</returns>
    private static Product? MapProduct(SURFSharekitRepoItem repoItem)
    {
        if (repoItem.Attributes?.Title is not { } title) //product requires a title to be made
            return null;

        Guid newId = Guid.NewGuid();
        Product mappedProduct = new()
        {
            Title = title,
            Id = newId,
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
        };

        return mappedProduct;
    }
}
