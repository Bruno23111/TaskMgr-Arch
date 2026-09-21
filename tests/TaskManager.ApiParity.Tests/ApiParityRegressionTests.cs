using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.WebAPI.Contracts.Requests;
using TaskManager.MVC.API.Configuration;
using TaskManager.MVC.API.Contracts.Auth;
using TaskManager.MVC.API.Identity;
using TaskManager.MVC.API.Services.Tasks;
using Xunit;

namespace TaskManager.ApiParity.Tests;

public sealed class ApiParityRegressionTests
{
    [Fact]
    public void ContratosHttpNaoUsamDataAnnotations()
    {
        var contractTypes = typeof(TaskManager.MVC.API.Contracts.Auth.LoginRequest).Assembly.GetTypes()
            .Concat(typeof(TaskManager.CleanArch.WebAPI.Contracts.Requests.LoginRequest).Assembly.GetTypes())
            .Where(type => type.Namespace?.Contains(".Contracts", StringComparison.Ordinal) == true)
            .ToArray();

        var attributes = contractTypes
            .SelectMany(type => type.GetProperties().SelectMany(property => property.GetCustomAttributes<ValidationAttribute>()))
            .Concat(contractTypes.SelectMany(type => type.GetConstructors())
                .SelectMany(constructor => constructor.GetParameters())
                .SelectMany(parameter => parameter.GetCustomAttributes<ValidationAttribute>()))
            .ToArray();

        Assert.Empty(attributes);
    }

    [Fact]
    public void CategoriaDeStatusNumericaEhRejeitadaNaCleanApi()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper, allowIntegerValues: false));

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<CriarStatusRequest>(
                """{"codigo":"TESTE","nome":"Teste","categoria":0}""",
                options));
    }

    [Fact]
    public void ReaberturaPorUsuarioRetornaAcessoNegadoNoMvc()
    {
        var exception = Assert.Throws<ApiException>(() =>
            TaskStatusTransition.Evaluate(
                TaskManager.MVC.API.Models.CategoriaStatus.Concluida,
                TaskManager.MVC.API.Models.CategoriaStatus.Pendente,
                isManager: false));

        Assert.Equal(403, exception.Status);
        Assert.Equal("ACESSO_NEGADO", exception.Code);
    }

    [Fact]
    public void CredenciaisInvalidasUsamCodigoPublicoDoContrato()
    {
        var exception = new CredenciaisInvalidasException();

        Assert.Equal("NAO_AUTENTICADO", exception.Code);
    }

    [Fact]
    public void AccountServiceEstaRegistradoNoMvc()
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:TaskManager"] = "Host=localhost;Database=test;Username=test;Password=test",
            ["Jwt:Emissor"] = "TaskMgr",
            ["Jwt:Audiencia"] = "TaskMgr",
            ["Jwt:ChaveSecreta"] = "01234567890123456789012345678901"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();

        services.AddTaskManagerMvc(configuration);

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(AccountService));
    }

    [Theory]
    [InlineData(typeof(TaskManager.MVC.API.Controllers.ProjectsController), "Criar")]
    [InlineData(typeof(TaskManager.MVC.API.Controllers.ProjectsController), "Atualizar")]
    [InlineData(typeof(TaskManager.MVC.API.Controllers.ProjectsController), "Excluir")]
    [InlineData(typeof(TaskManager.CleanArch.WebAPI.Controllers.ProjetosController), "Criar")]
    [InlineData(typeof(TaskManager.CleanArch.WebAPI.Controllers.ProjetosController), "Atualizar")]
    [InlineData(typeof(TaskManager.CleanArch.WebAPI.Controllers.ProjetosController), "Excluir")]
    public void MutacoesDeProjetoExigemGestorAntesDoCorpo(Type controller, string action)
    {
        var method = controller.GetMethod(action, BindingFlags.Instance | BindingFlags.Public);
        var authorization = method?.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorization);
        Assert.Equal("Gestor", authorization.Roles);
    }
}
