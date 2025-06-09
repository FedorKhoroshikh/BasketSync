using Application.DTO;
using AutoMapper;
using Domain.Entities;

namespace Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<ListItem, ListItemDto>()
            .ForMember(d => d.ItemName, c => c.MapFrom(s => s.Item.Name));

        CreateMap<ShoppingList, ShoppingListDto>();

        CreateMap<ListItem, ListItemDto>();
    }

        
}