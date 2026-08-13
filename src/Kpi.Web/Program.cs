using Kpi.Infrastructure.Postgres;
using Kpi.Web.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<Kpi.Application.Common.IClock, Kpi.Application.Common.SystemClock>();
builder.Services.AddSingleton<Kpi.Application.Formula.FormulaService>();
builder.Services.AddScoped<Kpi.Web.Queries.KpiWebReadModelService>();
builder.Services.AddScoped<Kpi.Web.Development.CurrentActorAccessor>();
builder.Services.AddScoped<Kpi.Application.Common.ICurrentActor>(sp => sp.GetRequiredService<Kpi.Web.Development.CurrentActorAccessor>());
if (builder.Environment.IsDevelopment())
    builder.Services.AddScoped<Kpi.Application.Persistence.IPlatformIdentityReader, Kpi.Application.Persistence.DevelopmentPlatformIdentityAdapter>();
PostgresRuntimeConfiguration.AddPersistence(builder.Services, builder.Configuration);

builder.Services.AddHostedService<Kpi.Web.HostedServices.KpiTimeReconciliationWorker>();

var app = builder.Build();
Kpi.Web.Bootstrap.BootstrapConfigurationCheck.Validate(app.Configuration, app.Environment);
app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

/// <summary>Exposes the generated host entry point to integration tests.</summary>
public partial class Program;
