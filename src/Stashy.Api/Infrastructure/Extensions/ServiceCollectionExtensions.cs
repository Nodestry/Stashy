﻿using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Stashy.Api.Infrastructure;
using Swashbuckle.AspNetCore.Swagger;

// ReSharper disable once CheckNamespace
namespace Stashy.Extensions
{
    public static class ServiceCollectionExtensions
    {
        private static string XmlCommentsFilePath
        {
            get
            {
                string basePath = AppContext.BaseDirectory;
                string fileName = typeof(Startup).GetTypeInfo()
                                      .Assembly.GetName()
                                      .Name + ".xml";

                return Path.Combine(basePath, fileName);
            }
        }

        public static void AddApiDocumentation(this IServiceCollection services)
        {
            // api versioning
            services.AddVersionedApiExplorer(options =>
            {
                // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
                // note: the specified format code will format the version as "'v'major[.minor][-status]"
                options.GroupNameFormat = "'v'VVV";

                // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
                // can also be used to control the format of the API version in route templates
                options.SubstituteApiVersionInUrl = true;
            });
            services.AddApiVersioning(options => options.ReportApiVersions = true);
            services.AddSwaggerGen(options =>
            {
                // resolve the IApiVersionDescriptionProvider service
                // note: that we have to build a temporary service provider here because one has not been created yet
                IApiVersionDescriptionProvider provider = services.BuildServiceProvider()
                    .GetRequiredService<IApiVersionDescriptionProvider>();

                // add a swagger document for each discovered API version
                // note: you might choose to skip or document deprecated API versions differently
                foreach (ApiVersionDescription description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
                }

                // add a custom operation filter which sets default values
                options.OperationFilter<SwaggerDefaultValues>();

                // integrate xml comments
                options.IncludeXmlComments(XmlCommentsFilePath);
            });
        }

        private static Info CreateInfoForApiVersion(ApiVersionDescription description)
        {
            Info info = new Info
            {
                Title = $"Stashy API {description.ApiVersion}",
                Version = description.ApiVersion.ToString(),
                Contact = new Contact {Name = "Nodestry", Email = "hi@nodestry.com", Url = "https://nodestry.com"}
            };

            if (description.IsDeprecated)
            {
                info.Description += " This API version has been deprecated.";
            }

            return info;
        }
    }
}
