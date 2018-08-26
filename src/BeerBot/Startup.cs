using System;
using BeerBot.BeerApiClient;
using BeerBot.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder.Ai.LUIS;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Cognitive.LUIS;
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
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddBot<BeerBot>(options =>
            {
                options.CredentialProvider = new ConfigurationCredentialProvider(Configuration);

                options.Middleware.Add(new CatchExceptionMiddleware<Exception>(async (context, exception) =>
                {
                    await context.TraceActivity("BeerBot Exception", exception);
                    await context.SendActivity("Sorry, it looks like something went wrong!");
                }));

                IStorage dataStore = new MemoryStorage();
                //IStorage dataStore = new AzureBlobStorage(Configuration.GetValue<string>("AzureTableConnectionString"), "beerstate");
                options.Middleware.Add(new ConversationState<BeerConversationState>(dataStore));
                options.Middleware.Add(new UserState<UserInfo>(dataStore));

                var modelId = Configuration.GetValue<string>("CognitiveServiceLuisAppId");
                options.Middleware.Add(
                    new LuisRecognizerMiddleware(
                        new LuisModel(
                            modelId,
                            Configuration.GetValue<string>("CognitiveServiceLuisApiKey"),
                            new Uri("https://westeurope.api.cognitive.microsoft.com/luis/v2.0/apps/")),
                        new LuisRecognizerOptions { Verbose = true },
                        new LuisRequest
                        {
                            SpellCheck = true,
                            BingSpellCheckSubscriptionKey = Configuration.GetValue<string>("CognitiveServiceBingSpellCheckApiKey")
                        }));

                options.Middleware.Add(new ShowTypingMiddleware());
            });

            services.AddSingleton<IBeerApi, BeerApi>(sp => new BeerApi(new Uri(Configuration.GetValue<string>("BeerApiBaseUrl"))));
            services.AddSingleton<IImageSearchService, ImageSearchService>(sp => new ImageSearchService(Configuration.GetValue<string>("CognitiveServiceBingSearchApiKey")));
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
