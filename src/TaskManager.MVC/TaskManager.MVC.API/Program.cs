using TaskManager.MVC.API.Configuration;
using TaskManager.MVC.API.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTaskManagerMvc(builder.Configuration);

var app = builder.Build();

await app.Services.InitializeIdentityAsync(app.Configuration);

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
