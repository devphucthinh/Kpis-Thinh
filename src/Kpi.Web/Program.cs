using Kpi.Infrastructure.Postgres;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<Kpi.Application.InMemoryKpiStore>();
builder.Services.AddSingleton<Kpi.Application.Common.IClock, Kpi.Application.Common.SystemClock>();
builder.Services.AddScoped<Kpi.Application.KpiOperations>();
builder.Services.AddScoped<Kpi.Application.PeriodOperations>();
builder.Services.AddScoped<Kpi.Application.EvaluationOperations>();
builder.Services.AddScoped<Kpi.Application.ReconcileKpiLifecycle>();
builder.Services.AddSingleton<Kpi.Application.Formula.FormulaService>();
builder.Services.AddScoped<Kpi.Web.Development.CurrentActorAccessor>();
builder.Services.AddScoped<Kpi.Application.Common.ICurrentActor>(sp => sp.GetRequiredService<Kpi.Web.Development.CurrentActorAccessor>());
builder.Services.AddHostedService<Kpi.Web.Development.DevelopmentSeedData>();
builder.Services.AddHostedService<Kpi.Web.HostedServices.KpiTimeReconciliationWorker>();
builder.Services.AddKpiPostgres(builder.Configuration);

var app = builder.Build();
Kpi.Web.Bootstrap.BootstrapConfigurationCheck.Validate(app.Configuration, app.Environment);
app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

/// <summary>Exposes the generated host entry point to integration tests.</summary>
public partial class Program;
