namespace Microsoft.Extensions.DependencyInjection;

using ECM.AccessControl.Application.Relations.Commands;
using ECM.AccessControl.Application.Relations.Queries;
using ECM.AccessControl.Application.Roles.Commands;
using ECM.AccessControl.Application.Roles.Queries;
using ECM.AccessControl.Application.Users.Commands;
using ECM.AccessControl.Application.Users.Queries;

public static class AccessControlApplicationModuleExtensions
{
    public static IServiceCollection AddAccessControlApplication(this IServiceCollection services)
    {
        services.AddScoped<GetUsersQueryHandler>();
        services.AddScoped<GetUserByIdQueryHandler>();
        services.AddScoped<GetUserByEmailQueryHandler>();
        services.AddScoped<CreateUserCommandHandler>();
        services.AddScoped<UpdateUserCommandHandler>();
        services.AddScoped<UpdateUserProfileCommandHandler>();
        services.AddScoped<AssignUserRoleCommandHandler>();
        services.AddScoped<RemoveUserRoleCommandHandler>();

        services.AddScoped<GetRolesQueryHandler>();
        services.AddScoped<GetRoleByIdQueryHandler>();
        services.AddScoped<CreateRoleCommandHandler>();
        services.AddScoped<RenameRoleCommandHandler>();
        services.AddScoped<DeleteRoleCommandHandler>();

        services.AddScoped<GetAccessRelationsBySubjectQueryHandler>();
        services.AddScoped<GetAccessRelationsByObjectQueryHandler>();
        services.AddScoped<CreateAccessRelationCommandHandler>();
        services.AddScoped<DeleteAccessRelationCommandHandler>();

        return services;
    }
}
