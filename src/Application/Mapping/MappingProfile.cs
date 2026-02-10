using Application.DTO;
using AutoMapper;
using Domain.Entities;

namespace Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<ListItem, ListItemDto>()
            .ForCtorParam("Comment", opt => opt.MapFrom(s => s.Comment))
            .ForCtorParam("ItemName", opt => opt.MapFrom(s => s.Item.Name))
            .ForCtorParam("CategoryId", opt => opt.MapFrom(s => s.Item.CategoryId))
            .ForCtorParam("CategoryName", opt => opt.MapFrom(s => s.Item.Category.Name))
            .ForCtorParam("UnitId", opt => opt.MapFrom(s => s.Item.UnitId))
            .ForCtorParam("UnitName", opt => opt.MapFrom(s => s.Item.Unit.Name))
            .ForMember(d => d.ItemName, c => c.MapFrom(s => s.Item.Name))
            .ForMember(d => d.CategoryId, c => c.MapFrom(s => s.Item.CategoryId))
            .ForMember(d => d.CategoryName, c => c.MapFrom(s => s.Item.Category.Name))
            .ForMember(d => d.UnitId, c => c.MapFrom(s => s.Item.UnitId))
            .ForMember(d => d.UnitName, c => c.MapFrom(s => s.Item.Unit.Name));

        CreateMap<ShoppingList, ShoppingListDto>()
            .ForCtorParam("Items", opt => opt.MapFrom(s => s.ListItems))
            .ForMember(d => d.Items, c => c.MapFrom(s => s.ListItems));

        CreateMap<Item, ItemDto>()
            .ForMember(d => d.CategoryName, c => c.MapFrom(s => s.Category.Name))
            .ForMember(d => d.UnitName, c => c.MapFrom(s => s.Unit.Name));

        CreateMap<Category, CategoryDto>();
        CreateMap<Unit, UnitDto>();
    }
}