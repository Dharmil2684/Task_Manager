using TaskManager.API.Middleware;
using TaskManager.Infrastructure;
using TaskManager.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container via Clean Architecture DI extensions
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
// Swagger UI — generate OpenAPI documents
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Ensure Cosmos DB Database & Containers exist
using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<CosmosDbInitializer>();
    await dbInitializer.InitializeAsync();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

// Enable Swagger (only in development ideally)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskManager API V1");
        c.RoutePrefix = string.Empty;
    });
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
