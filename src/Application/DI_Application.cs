using Application.Commands;
using Application.DTO;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DI_Application
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<AddListItemCommand>());
        services.AddAutoMapper(typeof(ItemDto).Assembly);
        return services;
    }
}