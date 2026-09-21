namespace TaskManager.MVC.API.Configuration;

public sealed class ApiException(int status, string code, string title, string detail) : Exception(detail)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
    public string Title { get; } = title;
}
