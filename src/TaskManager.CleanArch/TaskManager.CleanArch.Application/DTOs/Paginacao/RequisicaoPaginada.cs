namespace TaskManager.CleanArch.Application.DTOs.Paginacao;

public sealed record RequisicaoPaginada(int Pagina = 1, int Tamanho = 20)
{
    public const int TamanhoMaximo = 100;

    public int Deslocamento
    {
        get
        {
            Validar();
            return (Pagina - 1) * Tamanho;
        }
    }

    public void Validar()
    {
        if (Pagina < 1)
            throw new ArgumentOutOfRangeException(nameof(Pagina), "A página deve ser maior ou igual a 1.");
            
        if (Tamanho is < 1 or > TamanhoMaximo)
            throw new ArgumentOutOfRangeException(nameof(Tamanho), $"O tamanho deve estar entre 1 e {TamanhoMaximo}.");
    }
}
