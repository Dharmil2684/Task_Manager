using Microsoft.Extensions.DependencyInjection;
using TaskManager.Core.Interfaces;

namespace TaskManager.Services
{
    /// Registers all Application/Services-layer services into the DI container.
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ITenantContext, TenantContext>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IInvitationService, InvitationService>();

            return services;
        }
    }
}
