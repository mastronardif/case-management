using System.Reflection;

namespace WebAppMulti.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCorqs(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var strategyTypes = assembly.GetTypes()
            .Where(t =>
                !t.IsAbstract &&
                !t.IsInterface &&
                typeof(ICorqsExecutionStrategy).IsAssignableFrom(t));

        foreach (var type in strategyTypes)
        {
            services.AddScoped(typeof(ICorqsExecutionStrategy), type);
        }

        var handlerTypes = assembly.GetTypes()
            .Where(t =>
                !t.IsAbstract &&
                !t.IsInterface &&
                typeof(ICorqsHandler).IsAssignableFrom(t));

        foreach (var type in handlerTypes)
        {
            services.AddScoped(typeof(ICorqsHandler), type);
        }

        return services;
    }
}
