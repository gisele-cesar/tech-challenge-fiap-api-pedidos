using fiap.Domain.Entities;
using System.Threading.Tasks;

namespace fiap.Domain.Interfaces
{
    public interface IPagamentoService
    {
        Task<bool> CriarOrdemPagamento(Pedido pedido);
    }
}
