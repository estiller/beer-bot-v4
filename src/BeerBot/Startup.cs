using System;
using BeerBot.BeerApiClient;
using BeerBot.Dialogs;
using BeerBot.Dialogs.BeerOrdering;
using BeerBot.Dialogs.BeerRecommendation;
using BeerBot.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BeerBot
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();
            services.AddSingleton<IBotFrameworkHttpAdapter, BeerBotHttpAdapter>();
            services.AddSingleton<IStorage, MemoryStorage>();
            services.AddSingleton<ConversationState>();

            services.AddSingleton<MainDialog>();
            services.AddSingleton<RandomBeerDialog>();
            services.AddSingleton<RecommendBeerDialog>();
            services.AddSingleton<RecommendBeerByCategoryDialog>();
            services.AddSingleton<RecommendBeerByOriginDialog>();
            services.AddSingleton<RecommendBeerByNameDialog>();
            services.AddSingleton<SearchBeerForOrderDialog>();
            services.AddSingleton<RecommendationConversionDialog>();
            services.AddSingleton<OrderBeerDialog>();
            services.AddTransient<IBot, Bots.BeerBot>();

            services.AddSingleton<IBeerApi, BeerApi>(sp =>
            {
                var beerApiUrl = new Uri(_configuration.GetSection("BeerApiUrl").Value);
                return new BeerApi(beerApiUrl);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseAuthorization();

            app.UseWebSockets();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
