using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using samples.EcmFileIntegrationSample;
using Ecm.Sdk.Extensions;
using Ecm.Sdk.Configuration;
using EcmFileIntegrationSample.Services;

var builder = WebApplication.CreateBuilder(args);

var userStore = EcmUserStore.FromConfiguration(builder.Configuration);

builder.Services.AddSingleton(userStore);
builder.Services.AddSingleton<EcmUserSelection>();

builder.Services.AddSingleton<IPostConfigureOptions<EcmIntegrationOptions>>(serviceProvider =>
    new EcmUserOptionsConfigurator(serviceProvider.GetRequiredService<EcmUserSelection>()));

builder.Services.AddEcmSdk(options => EcmUserOptionsConfigurator.Copy(userStore.DefaultOptions, options));
builder.Services.AddScoped<IEcmIntegrationService, EcmIntegrationService>();

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
