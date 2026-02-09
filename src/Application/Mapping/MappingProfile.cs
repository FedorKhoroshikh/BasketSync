using Application.DTO;
using AutoMapper;
using Domain.Entities;

namespace Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<ListItem, ListItemDto>()
            .ForMember(d => d.ItemName, c => c.MapFrom(s => s.Item.Name))
            .ForMember(d => d.CategoryName, c => c.MapFrom(s => s.Item.Category.Name))
            .ForMember(d => d.UnitName, c => c.MapFrom(s => s.Item.Unit.Name));

        CreateMap<ShoppingList, ShoppingListDto>()
            .ForMember(d => d.Items, c => c.MapFrom(s => s.ListItems));

        CreateMap<Item, ItemDto>()
            .ForMember(d => d.CategoryName, c => c.MapFrom(s => s.Category.Name))
            .ForMember(d => d.UnitName, c => c.MapFrom(s => s.Unit.Name));

        CreateMap<Category, CategoryDto>();
        CreateMap<Unit, UnitDto>();
    }
}