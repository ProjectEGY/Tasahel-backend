using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace PostexS.Filters
{
    /// <summary>
    /// Swagger Operation Filter - يضيف Latitude و Longitude في الهيدر لكل الـ endpoints
    /// ويحسن عرض الباراميترز الموجودة
    /// </summary>
    public class AddHeaderParametersFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                operation.Parameters = new List<OpenApiParameter>();

            // تحسين الباراميترز الموجودة (Latitude, Longitude)
            foreach (var param in operation.Parameters)
            {
                if (param.Name == "Latitude" && param.In == ParameterLocation.Header)
                {
                    param.Description = "خط العرض (Latitude) - لتحديث موقع المندوب";
                    param.Required = false;
                    param.Schema = new OpenApiSchema
                    {
                        Type = "number",
                        Format = "double",
                        Example = new OpenApiDouble(30.0444)
                    };
                }
                else if (param.Name == "Longitude" && param.In == ParameterLocation.Header)
                {
                    param.Description = "خط الطول (Longitude) - لتحديث موقع المندوب";
                    param.Required = false;
                    param.Schema = new OpenApiSchema
                    {
                        Type = "number",
                        Format = "double",
                        Example = new OpenApiDouble(31.2357)
                    };
                }
            }

            // لو الـ endpoint مفيهوش Latitude/Longitude (زي Login, ForgetPassword) - ما نضيفهمش
            var hasLatitude = operation.Parameters.Any(p => p.Name == "Latitude");
            var hasLongitude = operation.Parameters.Any(p => p.Name == "Longitude");

            // ما نضيفش للـ endpoints اللي مش محتاجاهم
            if (!hasLatitude && !hasLongitude)
                return;
        }
    }
}
