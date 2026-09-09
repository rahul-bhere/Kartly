using Kartly.API.Extensions;
using Kartly.Application.DTOs.Products;
using Kartly.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kartly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IWebHostEnvironment _env;

    // Allowed image types/size for uploads — kept intentionally small and
    // strict since this endpoint writes files to disk from user input.
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public ProductsController(IProductService productService, IWebHostEnvironment env)
    {
        _productService = productService;
        _env = env;
    }

    // GET /api/products?query=phone&category=electronics
    // Public — this is the catalog the admin builds via this API,
    // separate from the dummy-API catalog shown elsewhere in the app.
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<ProductDto>>> GetAll([FromQuery] string? query, [FromQuery] string? category)
    {
        var products = await _productService.GetAllAsync(query, category);
        return Ok(products);
    }

    // GET /api/products/{id}
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDto>> GetById(Guid id)
    {
        var product = await _productService.GetByIdAsync(id);
        return Ok(product);
    }

    // POST /api/products
    // Admin-only: this is the endpoint the Admin Dashboard's "Create
    // Product" form calls.
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> Create(CreateProductDto dto)
    {
        var adminId = User.GetUserId();
        var created = await _productService.CreateAsync(adminId, dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT /api/products/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> Update(Guid id, UpdateProductDto dto)
    {
        var updated = await _productService.UpdateAsync(id, dto);
        return Ok(updated);
    }

    // DELETE /api/products/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _productService.DeleteAsync(id);
        return NoContent();
    }

    // POST /api/products/upload-image
    // Admin-only: the Admin Dashboard's "Create/Edit product" form calls
    // this BEFORE submitting the product itself — it uploads the image
    // file and gets back a URL, which then gets sent as thumbnailUrl in
    // the normal Create/Update calls above. Kept as a separate endpoint
    // (rather than accepting a file directly on Create/Update) so the
    // same uploaded image can be reused across an edit without
    // re-uploading, and so upload failures are isolated from the rest of
    // the product form.
    //
    // WHY THIS LIVES IN THE API LAYER, NOT APPLICATION/INFRASTRUCTURE:
    // Saving to wwwroot and building a URL from the current request's
    // host is a hosting/presentation concern (it needs IWebHostEnvironment
    // and Request.Host, both ASP.NET Core hosting types), not a business
    // rule — so unlike ProductService's actual CRUD logic, this stays
    // here rather than being routed through the Application layer.
    [HttpPost("upload-image")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file was uploaded." });

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(new { error = "Image must be 5 MB or smaller." });

        if (!AllowedContentTypes.Contains(file.ContentType))
            return BadRequest(new { error = "Only JPEG, PNG, WEBP, or GIF images are allowed." });

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var uploadsFolder = Path.Combine(webRoot, "uploads");
        Directory.CreateDirectory(uploadsFolder);

        // A random filename, not the client-supplied one — never trust
        // (or execute path logic on) a filename from user input directly.
        var extension = Path.GetExtension(file.FileName);
        var safeExtension = AllowedExtensionFor(file.ContentType, extension);
        var fileName = $"{Guid.NewGuid():N}{safeExtension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var url = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";
        return Ok(new { url });
    }

    private static string AllowedExtensionFor(string contentType, string clientExtension) => contentType switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/gif" => ".gif",
        _ => string.IsNullOrWhiteSpace(clientExtension) ? ".bin" : clientExtension
    };
}
