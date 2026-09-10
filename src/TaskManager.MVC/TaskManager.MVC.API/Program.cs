using Microsoft.EntityFrameworkCore;
using TaskManager.MVC.API.Configuration;
using TaskManager.MVC.API.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("TaskManager");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "A connection string 'ConnectionStrings:TaskManager' deve ser configurada por User Secrets ou variável de ambiente.");
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTaskManagerProblemDetails();
builder.Services.AddDbContext<TaskManagerMvcDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddHealthChecks().AddDbContextCheck<TaskManagerMvcDbContext>(name: "postgresql");

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskMgr MVC API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
