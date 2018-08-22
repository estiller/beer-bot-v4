using System;
using BeerBot.BeerApi.Dal;
using BeerBot.BeerApi.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BeerBot.BeerApi
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddSwaggerGen(c =>
            {
                c.OperationFilter<NormalizeOperationIdFilter>();
                c.SwaggerDoc("v1", new Info { Title = "Beer API", Version = "v1" });
            });

            services.AddSingleton<IRepository<Beer>, BeerRepository>();
            services.AddSingleton<IRepository<Brewery>, BreweryRepository>();
            services.AddSingleton<IRepository<Category>, CategoryRepository>();
            services.AddSingleton<IRepository<Style>, StyleRepository>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseMvc();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Beer API V1");
            });
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class NormalizeOperationIdFilter : IOperationFilter
        {
            public void Apply(Operation operation, OperationFilterContext context)
            {
                if (operation.OperationId.StartsWith("Api", StringComparison.OrdinalIgnoreCase))
                    operation.OperationId = operation.OperationId.Substring(3);
            }
        }

    }
}
