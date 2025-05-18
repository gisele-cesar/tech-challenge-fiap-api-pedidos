using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace fiap.Domain.Entities
{
    public class Pedido
    {
        public string IdPedido { get; set; }
        public Cliente Cliente { get; set; }
        public string Numero { get; set; }
        public StatusPedido StatusPedido { get; set; }
        public StatusPagamento StatusPagamento { get; set; }
        public List<Produto> Produtos { get; set; }
        public decimal ValorTotal
        {
            get { return Produtos?.Select(p => p.Preco).Sum() ?? 0; }
        }
        public string DataCriacao { get; set; }
        public string DataAlteracao { get; set; }
    }

}
