namespace AssafTech.Extensions.DependencyInjection;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddCommonApplicationServices(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.Configure<Gateway>(configuration.GetSection(nameof(Gateway)));
        return services;
    }
    public static IServiceCollection AddSwagger(this IServiceCollection services, SwaggerModel swaggerModel)
    {
        var gateway = services.BuildServiceProvider().GetService<IOptions<Gateway>>();
        if (gateway == null) { throw new ArgumentNullException(nameof(gateway)); }

        services.AddSwaggerGen(option =>
        {
            option.SwaggerDoc(swaggerModel.Version, new OpenApiInfo { Title = swaggerModel.Title, Version = swaggerModel.Version });
            option.AddSecurityDefinition(SecuritySchemeType.OAuth2.GetDisplayName(), new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri($"{gateway.Value.BaseUrl}/{ServiceName.Identity}/connect/authorize"),
                        TokenUrl = new Uri($"{gateway.Value.BaseUrl}/{ServiceName.Identity}/connect/token"),
                        Scopes = swaggerModel.IdentityServerScopes
					}
                }
            });
            option.OperationFilter<AuthorizeCheckOperationFilter>();
            option.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type= ReferenceType.SecurityScheme,
                            Id= JwtBearerDefaults.AuthenticationScheme
                        }
                    },
                    new string[]{}
                }
            });
        });

        return services;
    }
    public static IServiceCollection AddAuth(this IServiceCollection services)
    {
        var gateway = services.BuildServiceProvider().GetService<IOptions<Gateway>>();
        if (gateway == null) { throw new ArgumentNullException(nameof(gateway)); }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = $"{gateway.Value.BaseUrl}/{ServiceName.Identity}";
                    //options.ApiName = IdentityServerApi.Name.Datalist; // its the resource api name (its needed when there is a communication btw this api and another api internally)
                    options.RequireHttpsMetadata = false;
                });

        return services;
    }
}

public class AuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context?.MethodInfo?.DeclaringType == null) return;

        var hasAuthorize =
          context.MethodInfo.DeclaringType.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any()
          || context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any();

        if (hasAuthorize)
        {
            operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
            operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });

            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new OpenApiSecurityRequirement
                {
                    [
                        new OpenApiSecurityScheme {Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = SecuritySchemeType.OAuth2.GetDisplayName()}
                        }
                    ] = new[] {IdentityServerApi.Name.Identity }
                }
            };

        }
    }
}