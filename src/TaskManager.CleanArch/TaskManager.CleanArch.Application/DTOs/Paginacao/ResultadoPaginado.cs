namespace TaskManager.CleanArch.Application.DTOs.Paginacao;

public sealed record ResultadoPaginado<T>(IReadOnlyCollection<T> Itens, int Pagina, int Tamanho, int Total)
{
    public static ResultadoPaginado<T> Criar(IReadOnlyCollection<T> itens, RequisicaoPaginada requisicao, int total)
    {
        requisicao.Validar();
        if (total < 0)
            throw new ArgumentOutOfRangeException(nameof(total));

        return new ResultadoPaginado<T>(itens, requisicao.Pagina, requisicao.Tamanho, total);
    }
}
