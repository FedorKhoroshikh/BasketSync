using Application.Commands;
using Application.DTO;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DI_Application
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<AddItemCommand>());
        services.AddAutoMapper(typeof(ItemDto).Assembly);
        return services;
    }
}