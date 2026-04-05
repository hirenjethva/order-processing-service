using Microsoft.AspNetCore.Mvc;
using OrderProcessingService.Application.Services;
using OrderProcessingService.Domain.Entities;

namespace OrderProcessingService.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductCatalogService _productCatalog;

    public ProductsController(IProductCatalogService productCatalog)
    {
        _productCatalog = productCatalog;
    }

    /// <summary>Returns all products (Redis cache-aside, then MongoDB).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Product>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Product>>> GetProducts()
    {
        var products = await _productCatalog.GetAllAsync().ConfigureAwait(false);
        return Ok(products);
    }

    /// <summary>Returns a single product by id (Redis cache-aside, then MongoDB).</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Product>> GetProduct([FromRoute] string id)
    {
        var product = await _productCatalog.GetByIdAsync(id).ConfigureAwait(false);
        if (product is null)
            return NotFound();

        return Ok(product);
    }
}
