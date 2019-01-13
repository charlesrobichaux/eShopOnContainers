using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ordering.API.Infrastructure.Filters
{
    public class AuthorizeCheckOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            // Check for authorize attribute
            if (!context.ApiDescription.TryGetMethodInfo(out var methodInfo)) return;
            var customAttributes = methodInfo.DeclaringType.GetCustomAttributes(true);
            var hasAuthorize = customAttributes.OfType<AuthorizeAttribute>().Any();

            if (hasAuthorize)
            {
                operation.Responses.Add("401", new Response { Description = "Unauthorized" });
                operation.Responses.Add("403", new Response { Description = "Forbidden" });

                operation.Security = new List<IDictionary<string, IEnumerable<string>>>
                {
                    new Dictionary<string, IEnumerable<string>> {{"oauth2", new[] {"orderingapi"}}}
                };
            }
            // Had to add below when upgrading Swashbuckle to 2.8.0.
            // see https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/603
            //operation.Security = new List<OpenApiSecurityRequirement>()
            //{
            //    new OpenApiSecurityRequirement()
            //    {
            //        new OpenApiSecurityScheme(),
            //    }
            //};
        }
    }
}