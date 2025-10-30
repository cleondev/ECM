using ECM.IAM.Application.Relations.Commands;
using ECM.IAM.Application.Relations.Queries;
using ECM.IAM.Application.Roles.Commands;
using ECM.IAM.Application.Roles.Queries;
using ECM.IAM.Application.Users.Commands;
using ECM.IAM.Application.Users.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.IAM.Application;
public static class IamApplicationModuleExtensions
{
    public static IServiceCollection AddIamApplication(this IServiceCollection services)
    {
        services.AddScoped<GetUsersQueryHandler>();
        services.AddScoped<GetUserByIdQueryHandler>();
        services.AddScoped<GetUserByEmailQueryHandler>();
        services.AddScoped<AuthenticateUserQueryHandler>();
        services.AddScoped<CreateUserCommandHandler>();
        services.AddScoped<UpdateUserCommandHandler>();
        services.AddScoped<UpdateUserProfileCommandHandler>();
        services.AddScoped<UpdateUserPasswordCommandHandler>();
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
