using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using samples.EcmFileIntegrationSample;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<EcmIntegrationOptions>(builder.Configuration.GetSection("Ecm"));
builder.Services.AddScoped<EcmAccessTokenProvider>();
builder.Services.AddTransient<EcmAccessTokenHandler>();
builder.Services.AddScoped<EcmOnBehalfAuthenticator>();
builder.Services.AddSingleton(new CookieContainer());

builder.Services.AddHttpClient<EcmFileClient>((serviceProvider, client) =>
    {
        var options = serviceProvider.GetRequiredService<IOptions<EcmIntegrationOptions>>().Value;

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            throw new InvalidOperationException(
                "Ecm:BaseUrl must be configured in appsettings.json or environment variables.");
        }

        client.BaseAddress = new Uri(options.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(100);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ecm-integration-sample/1.0");
    })
    .AddHttpMessageHandler<EcmAccessTokenHandler>()
    .ConfigurePrimaryHttpMessageHandler(serviceProvider => new HttpClientHandler
    {
        CookieContainer = serviceProvider.GetRequiredService<CookieContainer>(),
        UseCookies = true,
    });

builder.Services.AddHttpContextAccessor();

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
