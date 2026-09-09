using AutoMapper;
using Kartly.Application.DTOs.Products;
using Kartly.Application.Exceptions;
using Kartly.Application.Interfaces;
using Kartly.Domain.Entities;

namespace Kartly.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProductService(IProductRepository productRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<ProductDto>> GetAllAsync(string? query, string? category)
    {
        var products = await _productRepository.SearchAsync(query, category);
        return _mapper.Map<List<ProductDto>>(products);
    }

    public async Task<ProductDto> GetByIdAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(Product), id);
        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> CreateAsync(Guid adminUserId, CreateProductDto dto)
    {
        var product = new Product
        {
            Title = dto.Title,
            Description = dto.Description,
            Price = dto.Price,
            DiscountPercentage = dto.DiscountPercentage,
            Stock = dto.Stock,
            Category = dto.Category,
            Brand = dto.Brand,
            ThumbnailUrl = dto.ThumbnailUrl,
            Rating = 0,
            CreatedByUserId = adminUserId
        };

        await _productRepository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto)
    {
        var product = await _productRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(Product), id);

        product.Title = dto.Title;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.DiscountPercentage = dto.DiscountPercentage;
        product.Stock = dto.Stock;
        product.Category = dto.Category;
        product.Brand = dto.Brand;
        product.ThumbnailUrl = dto.ThumbnailUrl;
        product.UpdatedAt = DateTime.UtcNow;

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ProductDto>(product);
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(Product), id);

        _productRepository.Remove(product);
        await _unitOfWork.SaveChangesAsync();
    }
}
