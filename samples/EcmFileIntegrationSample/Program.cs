using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;

using samples.EcmFileIntegrationSample;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<EcmIntegrationOptions>(builder.Configuration.GetSection("Ecm"));
builder.Services.AddScoped<EcmAccessTokenProvider>();
builder.Services.AddTransient<EcmAccessTokenHandler>();

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
    .AddHttpMessageHandler<EcmAccessTokenHandler>();

var azureAdSection = builder.Configuration.GetSection("AzureAd");
var ecmOptions = builder.Configuration.GetSection("Ecm").Get<EcmIntegrationOptions>() ?? new EcmIntegrationOptions();
var enableAzureSso = ecmOptions.UseAzureSso;

if (enableAzureSso)
{
    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddMicrosoftIdentityWebApp(azureAdSection)
        .EnableTokenAcquisitionToCallDownstreamApi()
        .AddInMemoryTokenCaches();

    builder.Services.AddAuthorization();
}

builder.Services.AddHttpContextAccessor();

builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

if (enableAzureSso)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
