using Unity;
using Unity.Microsoft.DependencyInjection;
using SchoolManagement.Common.Services;
using SchoolManagement.DbInfrastructure.Repositories.Implementations;
using SchoolManagement.DbInfrastructure.Repositories.Interfaces;
using SchoolManagement.Services.Implementations;
using SchoolManagement.Services.Implementations.Channels;
using SchoolManagement.Services.Interfaces;
using SchoolManagement.API.Services;

namespace SchoolManagement.API.Extensions;

public static class UnityContainerConfigurator
{
    public static IUnityContainer ConfigureUnity(this IUnityContainer container)
    {
        // Services
        container.RegisterType<IAuthService, AuthService>();
        container.RegisterType<IAuditLogService, AuditLogService>();
        container.RegisterType<ISchoolService, SchoolService>();
        container.RegisterType<IUserManagementService, UserManagementService>();
        container.RegisterType<IOrgStorageConfigService, OrgStorageConfigService>();
        container.RegisterType<INotificationConfigService, NotificationConfigService>();
        container.RegisterType<INotificationTemplateService, NotificationTemplateService>();
        container.RegisterType<INotificationService, NotificationService>();
        container.RegisterType<IInAppNotificationService, InAppNotificationService>();
        // Channel handlers registered as IEnumerable<INotificationChannelHandler>
        container.RegisterType<INotificationChannelHandler, EmailChannelHandler>("Email");
        container.RegisterType<INotificationChannelHandler, SmsChannelHandler>("SMS");
        container.RegisterType<INotificationChannelHandler, PushChannelHandler>("Push");
        container.RegisterType<INotificationChannelHandler, InAppChannelHandler>("InApp");

        // Common Services
        container.RegisterType<IEncryptionService, EncryptionService>();
        container.RegisterType<IRequestContext, RequestContext>();
        container.RegisterType<IRealtimeNotificationPusher, SignalRNotificationPusher>();

        // Repositories
        container.RegisterType(typeof(IWriteRepository<>), typeof(WriteRepository<>));
        container.RegisterType<IReadRepository, ReadRepository>();

        return container;
    }

    /// <summary>
    /// Integrates the Unity container with the ASP.NET Core host.
    /// Usage in Program.cs:
    ///   builder.Host.UseUnityServiceProvider(container => container.ConfigureUnity());
    /// This replaces the built-in DI with Unity while preserving all existing registrations.
    /// </summary>
    public static IHostBuilder UseUnityServiceProvider(this IHostBuilder hostBuilder)
    {
        var container = new UnityContainer();
        container.ConfigureUnity();
        return hostBuilder.UseUnityServiceProvider(container);
    }
}
