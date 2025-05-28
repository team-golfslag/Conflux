// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.Exceptions;

public class ProductNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProductNotFoundException" /> class.
    /// </summary>
    /// <param name="projectId">The ID of the project that was given</param>
    /// <param name="productId">The ID of the product that was not found</param>
    public ProductNotFoundException(Guid projectId, Guid productId)
        : base($"Product with ID {productId} and Project ID {projectId} was not found.")
    {
    }
}
