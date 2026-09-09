using AutoMapper;
using Kartly.Application.DTOs.Cart;
using Kartly.Application.DTOs.Orders;
using Kartly.Application.DTOs.Payments;
using Kartly.Application.DTOs.Products;
using Kartly.Application.DTOs.Users;
using Kartly.Domain.Entities;

namespace Kartly.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.Role, opt => opt.MapFrom(s => s.Role.ToString()));

        CreateMap<Product, ProductDto>();

        CreateMap<CartItem, CartItemDto>()
            .ForMember(d => d.ProductTitle, opt => opt.MapFrom(s => s.TitleSnapshot))
            .ForMember(d => d.ThumbnailUrl, opt => opt.MapFrom(s => s.ThumbnailUrl))
            .ForMember(d => d.UnitPrice, opt => opt.MapFrom(s => s.UnitPriceSnapshot));

        CreateMap<Cart, CartDto>();

        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(d => d.ProductTitle, opt => opt.MapFrom(s => s.ProductNameSnapshot))
            .ForMember(d => d.UnitPrice, opt => opt.MapFrom(s => s.UnitPriceSnapshot));

        CreateMap<Order, OrderDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => $"{s.User.FirstName} {s.User.LastName}"))
            .ForMember(d => d.CustomerUsername, opt => opt.MapFrom(s => s.User.Username));

        CreateMap<Payment, PaymentDto>()
            .ForMember(d => d.Method, opt => opt.MapFrom(s => s.Method.ToString()))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));
    }
}
