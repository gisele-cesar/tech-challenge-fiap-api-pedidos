using System.Collections.Generic;
using fiap.Domain.Entities;

namespace fiap.Application.DTO
{
    public class PedidoDTO
    {
        public string IdPedido { get; set; }
        public string NumeroPedido { get; set; }
        public Cliente Cliente { get; set; }
        public StatusPedido StatusPedido { get; set; }
        public StatusPagamento StatusPagamento { get; set; }
        public List<Produto> Produtos { get; set; }
    }
}
