using TaskManager.CleanArch.Application.DependencyInjection;
using TaskManager.CleanArch.WebAPI.Configuration;
using TaskManager.CleanArch.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTaskManagerProblemDetails();
builder.Services.AddTaskManagerApplication();
builder.Services.AddTaskManagerInfrastructure(builder.Configuration);
builder.Services.AddTaskManagerWebApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskMgr Clean Architecture API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
