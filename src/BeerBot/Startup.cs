using System;
using System.Linq;
using BeerBot.BeerApiClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeerBot
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            if (env.IsDevelopment())
            {
                builder.AddUserSecrets<Startup>();
            }

            Configuration = builder.Build();

            var secretKey = Configuration.GetSection("botFileSecret")?.Value;
            var botFilePath = Configuration.GetSection("botFilePath")?.Value;
            BotConfiguration = BotConfiguration.Load(botFilePath ?? @".\BeerBot.bot", secretKey) ??
                               throw new InvalidOperationException($"The .bot config file could not be loaded. ({botFilePath})");
        }

        public IConfiguration Configuration { get; }

        public BotConfiguration BotConfiguration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddBot<BeerBot>(options =>
            {
                var endpointService = (EndpointService) BotConfiguration.Services.First(s => s.Type == "endpoint");

                options.CredentialProvider = new SimpleCredentialProvider(endpointService.AppId, endpointService.AppPassword);

                options.OnTurnError = async (context, exception) =>
                {
                    await context.TraceActivityAsync("BeerBot Exception", exception);
                    await context.SendActivityAsync("Sorry, it looks like something went wrong!");
                };
            });

            services.AddSingleton<IBeerApi, BeerApi>(sp =>
            {
                var beerApiConfig = (GenericService) BotConfiguration.Services.First(service => service.Name == "BeerApi");
                return new BeerApi(new Uri(beerApiConfig.Url));
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseBotFramework();
        }
    }
}
