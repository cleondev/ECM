using Ecm.Sdk.Authentication;
using Ecm.Sdk.Configuration;
using Ecm.Sdk.Extensions;

using EcmFileIntegrationSample;
using EcmFileIntegrationSample.Services;

using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var userStore = EcmUserStore.FromConfiguration(builder.Configuration);

builder.Services.AddSingleton(userStore);
builder.Services.AddSingleton<EcmUserSelection>();

builder.Services.AddSingleton<IPostConfigureOptions<EcmIntegrationOptions>>(serviceProvider =>
    new EcmUserOptionsConfigurator(serviceProvider.GetRequiredService<EcmUserSelection>()));

builder.Services.AddEcmSdk(options => EcmUserOptionsConfigurator.Copy(userStore.DefaultOptions, options));
builder.Services.AddScoped<IEcmIntegrationService, EcmIntegrationService>();
builder.Services.AddScoped<IEcmUserContext, EnvEcmUserContext>();


builder.Services.AddHttpContextAccessor();

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.Use(async (context, next) =>
{
    var userSelection = context.RequestServices.GetRequiredService<EcmUserSelection>();

    // Ensure antiforgery tokens are generated/validated against the selected (or default) user
    // by setting the HttpContext.User at the start of every request.
    userSelection.SelectUser();

    await next();
});

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
