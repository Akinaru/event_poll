using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EventPoll
{
    internal class SecurityRequirementsOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context == null || operation == null)
            {
                return;
            }

            var authorizeAttr =
                context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().FirstOrDefault()
                ?? context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>().FirstOrDefault();

            if (authorizeAttr != null)
            {
                operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });

                if (!string.IsNullOrEmpty(authorizeAttr.Policy) || !string.IsNullOrEmpty(authorizeAttr.Roles))
                {
                    operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });
                }

                operation.Security = new List<OpenApiSecurityRequirement>
                {
                    new OpenApiSecurityRequirement()
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            new string[] {}
                        }
                    }
                };
            }
        }
    }
}
