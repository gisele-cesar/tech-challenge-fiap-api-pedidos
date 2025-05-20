using fiap.Application;
using fiap.Repositories;
using fiap.Services;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

[assembly: ExcludeFromCodeCoverage]

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "FIAP - Tech Challenge - API Pedidos", Version = "v1" });
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        c.IncludeXmlComments(xmlPath);
    });
builder.Services.AddHealthChecks();

// Adiciona injeção de dependência no Application
builder.Services.AddHttpClient();
builder.Services.AddApplicationModule();
builder.Services.AddServicesModule();

builder.Services.AddRepositoriesModule();
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(opt =>
{
    opt.SwaggerEndpoint("/swagger/v1/swagger.json", "FIAP - Tech Challenge V1");
});


app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseAuthorization();
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapHealthChecks("api/health");
    endpoints.MapHealthChecks("api/metrics");
    endpoints.MapControllers();
});

app.UsePathBase("api-pedidos");
app.MapControllers();
app.Map("/", app1 => app1
.Run(ctx => ctx.Response
.WriteAsync($"path-Base: {ctx.Request.PathBase} Path: {ctx.Request.Path}")));

Log.Information("Iniciando aplicação");
app.Run();


