using AutoMapper;
using Kartly.Application.DTOs.Cart;
using Kartly.Application.Exceptions;
using Kartly.Application.Interfaces;
using Kartly.Domain.Entities;

namespace Kartly.Application.Services;

public class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    // WHY EVERY MUTATING METHOD RETRIES:
    // Two requests touching the SAME user's cart close together (a
    // double-click before a button disables, a page that fires an add
    // right after a background refresh, etc.) can each load the cart,
    // then race to save — the second save can find the row it expected
    // to update already changed or gone, which UnitOfWork.SaveChangesAsync
    // reports as a ConcurrencyConflictException (translated from EF Core's
    // own exception type at the Infrastructure boundary — see UnitOfWork.cs
    // — so this Application-layer file never needs to reference EF Core at
    // all). Rather than surface that as a user-
    // facing error ("please refresh and try again"), each method below
    // catches it, resets the DbContext's tracking (see
    // IUnitOfWork.ResetTracking), and retries the WHOLE load-mutate-save
    // sequence against fresh data — up to MaxAttempts times. This is the
    // standard EF Core recommended pattern for transient optimistic-
    // concurrency conflicts, and it makes "add to cart" self-healing
    // instead of something the shopper has to notice and retry manually.
    private const int MaxAttempts = 3;

    public CartService(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<CartDto> GetCartAsync(Guid userId)
    {
        var cart = await GetOrCreateCartAsync(userId);
        return _mapper.Map<CartDto>(cart);
    }

    public async Task<CartDto> AddItemAsync(Guid userId, AddCartItemDto dto)
    {
        if (dto.Quantity < 1)
            throw new ValidationAppException("Quantity must be at least 1.");

        Product? realProduct = null;
        if (dto.ProductId.HasValue)
        {
            realProduct = await _productRepository.GetByIdAsync(dto.ProductId.Value)
                ?? throw new NotFoundException(nameof(Product), dto.ProductId.Value);
        }
        else if (string.IsNullOrWhiteSpace(dto.ExternalRef))
        {
            throw new ValidationAppException("Either productId or externalRef must be provided.");
        }
        else if (string.IsNullOrWhiteSpace(dto.Title) || dto.UnitPrice is null)
        {
            throw new ValidationAppException("Title and unitPrice are required when adding an external (dummy-catalog) item.");
        }

        return await RetryOnConcurrencyConflict(async () =>
        {
            var cart = await GetOrCreateCartAsync(userId);

            if (realProduct is not null)
            {
                var existing = cart.Items.FirstOrDefault(i => i.ProductId == realProduct.Id);
                if (existing is not null)
                {
                    existing.Quantity += dto.Quantity;
                }
                else
                {
                    cart.Items.Add(new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = realProduct.Id,
                        TitleSnapshot = realProduct.Title,
                        UnitPriceSnapshot = realProduct.Price,
                        ThumbnailUrl = realProduct.ThumbnailUrl,
                        Quantity = dto.Quantity
                    });
                }
            }
            else
            {
                var existing = cart.Items.FirstOrDefault(i => i.ExternalRef == dto.ExternalRef);
                if (existing is not null)
                {
                    existing.Quantity += dto.Quantity;
                }
                else
                {
                    cart.Items.Add(new CartItem
                    {
                        CartId = cart.Id,
                        ExternalRef = dto.ExternalRef,
                        TitleSnapshot = dto.Title!,
                        UnitPriceSnapshot = dto.UnitPrice!.Value,
                        ThumbnailUrl = dto.ThumbnailUrl,
                        Quantity = dto.Quantity
                    });
                }
            }

            await _unitOfWork.SaveChangesAsync();

            var refreshed = await _cartRepository.GetByUserIdWithItemsAsync(userId);
            return _mapper.Map<CartDto>(refreshed!);
        });
    }

    public async Task<CartDto> UpdateItemAsync(Guid userId, Guid cartItemId, UpdateCartItemDto dto)
    {
        if (dto.Quantity < 1)
            throw new ValidationAppException("Quantity must be at least 1. Use the remove endpoint to delete an item.");

        return await RetryOnConcurrencyConflict(async () =>
        {
            var cart = await GetOrCreateCartAsync(userId);
            var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId)
                ?? throw new NotFoundException("CartItem", cartItemId);

            item.Quantity = dto.Quantity;
            await _unitOfWork.SaveChangesAsync();

            var refreshed = await _cartRepository.GetByUserIdWithItemsAsync(userId);
            return _mapper.Map<CartDto>(refreshed!);
        });
    }

    public async Task<CartDto> RemoveItemAsync(Guid userId, Guid cartItemId)
    {
        return await RetryOnConcurrencyConflict(async () =>
        {
            var cart = await GetOrCreateCartAsync(userId);
            var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);

            // Already gone (e.g. a concurrent request removed it first) —
            // treat as success rather than an error; the end state the
            // caller wanted (item not in cart) is already true.
            if (item is not null)
            {
                cart.Items.Remove(item);
                await _unitOfWork.SaveChangesAsync();
            }

            var refreshed = await _cartRepository.GetByUserIdWithItemsAsync(userId);
            return _mapper.Map<CartDto>(refreshed!);
        });
    }

    public async Task ClearCartAsync(Guid userId)
    {
        await RetryOnConcurrencyConflict<object?>(async () =>
        {
            var cart = await GetOrCreateCartAsync(userId);
            cart.Items.Clear();
            await _unitOfWork.SaveChangesAsync();
            return null;
        });
    }

    private async Task<Cart> GetOrCreateCartAsync(Guid userId)
    {
        var cart = await _cartRepository.GetByUserIdWithItemsAsync(userId);
        if (cart is not null) return cart;

        // Every account already gets a Cart created at registration (see
        // AuthService.RegisterAsync / DbSeeder), so reaching here is a
        // fallback for accounts created before that existed. Guard against
        // two concurrent first-ever requests both trying to create one —
        // Cart.UserId has a unique index (one-to-one with User), so the
        // loser of that race gets a constraint violation, not silence;
        // catching it and re-querying returns the winner's row instead of
        // failing the request.
        try
        {
            cart = new Cart { UserId = userId };
            await _cartRepository.AddAsync(cart);
            await _unitOfWork.SaveChangesAsync();
            return cart;
        }
        catch (ConcurrencyConflictException ex)
        {
            _unitOfWork.ResetTracking();
            var existing = await _cartRepository.GetByUserIdWithItemsAsync(userId);
            return existing ?? throw ex;
        }
    }

    private async Task<T> RetryOnConcurrencyConflict<T>(Func<Task<T>> operation)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (ConcurrencyConflictException) when (attempt < MaxAttempts)
            {
                _unitOfWork.ResetTracking();
            }
        }

        // Unreachable in practice (the last attempt either returns or lets
        // the exception propagate), but satisfies the compiler.
        return await operation();
    }
}
