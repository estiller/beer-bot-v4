using System;
using System.Linq;
using BeerBot.BeerApiClient;
using BeerBot.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BeerBot
{
    public class Startup
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public Startup(IHostingEnvironment env)
        {
            _hostingEnvironment = env;

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
                var endpointService = (EndpointService) BotConfiguration.Services.First(s => s.Type == "endpoint" && s.Name == _hostingEnvironment.EnvironmentName);

                options.CredentialProvider = new SimpleCredentialProvider(endpointService.AppId, endpointService.AppPassword);

                options.OnTurnError = async (context, exception) =>
                {
                    await context.TraceActivityAsync("BeerBot Exception", exception);
                    await context.SendActivityAsync("Sorry, it looks like something went wrong!");
                };

                IStorage dataStore = new MemoryStorage();
                //IStorage dataStore = new AzureBlobStorage(CONNECTION_STRING, "beerstate");
                var conversationState = new ConversationState(dataStore);
                options.State.Add(conversationState);
                var userState = new UserState(dataStore);
                options.State.Add(userState);
                options.Middleware.Add(new AutoSaveStateMiddleware(conversationState, userState));

                options.Middleware.Add(new ShowTypingMiddleware());
            });

            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<BotFrameworkOptions>>().Value;
                var conversationState = options.State.OfType<ConversationState>().First();
                var userState = options.State.OfType<UserState>().First();
                return new BeerBotAccessors(conversationState, userState)
                {
                    DialogState = conversationState.CreateProperty<DialogState>(BeerBotAccessors.DialogStateName),
                    UserInfo = conversationState.CreateProperty<UserInfo>(BeerBotAccessors.UserInfoName)
                };
            });

            services.AddSingleton<IBeerApi, BeerApi>(sp =>
            {
                var beerApiConfig = (GenericService) BotConfiguration.Services.First(service => service.Name == $"BeerApi-{_hostingEnvironment.EnvironmentName}");
                return new BeerApi(new Uri(beerApiConfig.Url));
            });
            services.AddSingleton<IImageSearchService, ImageSearchService>(sp =>
            {
                var imageSearchConfig = (GenericService)BotConfiguration.Services.First(service => service.Name == "ImageSearch");
                return new ImageSearchService(imageSearchConfig.Url, imageSearchConfig.Configuration["key"]);
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
