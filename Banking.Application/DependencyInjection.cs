using Banking.Application.Interfaces;
using Banking.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Banking.Application;

/// <summary>Registers the application services.</summary>
public static class DependencyInjection
{
    /// <summary>Adds the application services and a default <see cref="TimeProvider"/>.</summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IBankAccountService, BankAccountService>();
        services.AddScoped<ITransactionService, TransactionService>();

        return services;
    }
}
