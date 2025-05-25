using fiap.API.Controllers;
using fiap.Application.DTO;
using fiap.Application.Interfaces;
using fiap.Domain.Entities;
using fiap.Domain.Interfaces;
using Moq;
using Xunit;

namespace fiap.Tests.ControllerTests
{
    public class ControllerPedidoTests
    {
        private readonly Pedido pedido;
        private readonly PedidoDTO pedidoDTO;
        private readonly List<Pedido> lstPedido = [];
        public ControllerPedidoTests()
        {
            pedido = new Pedido
            {
                Cliente = new Cliente { Cpf = "12345678910", Email = "teste@teste.com", Id = 1, Nome = "Joao da Silva" },
                IdPedido = Guid.NewGuid().ToString(),
                Numero = "123456",
                StatusPagamento = StatusPagamento.Pendente,
                StatusPedido = StatusPedido.Solicitado
                 ,
                Produtos = [
                     new() { Preco = 1 , Nome = "teste" , IdProduto = 1, IdCategoriaProduto = 1 , Descricao = "teste" , DataCriacao = DateTime.Now , DataAlteracao = DateTime.Now },
                      new() { Preco = 2 , Nome = "teste2" , IdProduto = 2, IdCategoriaProduto = 2 , Descricao = "teste2" , DataCriacao = DateTime.Now , DataAlteracao = DateTime.Now }
                 ]
            };
            lstPedido = [
                pedido,
                pedido
            ];
            pedidoDTO = new PedidoDTO
            {
                Cliente = new Cliente { Cpf = "12345678910", Email = "teste@teste.com", Id = 1, Nome = "Joao da Silva" },
                StatusPagamento = StatusPagamento.Pendente,
                Produtos = [
                     new() { Preco = 1 , Nome = "teste" , IdProduto = 1, IdCategoriaProduto = 1 , Descricao = "teste" , DataCriacao = DateTime.Now , DataAlteracao = DateTime.Now },
                      new() { Preco = 2 , Nome = "teste2" , IdProduto = 2, IdCategoriaProduto = 2 , Descricao = "teste2" , DataCriacao = DateTime.Now , DataAlteracao = DateTime.Now }
                 ],
                NumeroPedido = "123"

            };
        }

        [Fact]
        public async Task Get_OKAsync()
        {
            var _application = new Mock<IPedidoApplication>();
            var _logger = new Mock<Serilog.ILogger>();
            var _pagamentoService = new Mock<IPagamentoService>();

            _pagamentoService.SetupSequence(x => x.CriarOrdemPagamento(It.IsAny<Pedido>())).ReturnsAsync(true);
            _application.SetupSequence(x => x.ObterPedidos()).ReturnsAsync(lstPedido);

            PedidoController controller = new(_logger.Object , _application.Object, _pagamentoService.Object);
            var result = await controller.Get();

            Assert.NotNull(result);
        }
        [Fact]
        public async Task Get_obterporStatus_OKAsync()
        {
            var _application = new Mock<IPedidoApplication>();
            var _logger = new Mock<Serilog.ILogger>();
            var _pagamentoService = new Mock<IPagamentoService>();

            _pagamentoService.SetupSequence(x => x.CriarOrdemPagamento(It.IsAny<Pedido>())).ReturnsAsync(true);
            _application.SetupSequence(x => x.ObterPedidosPorStatus("","","")).ReturnsAsync(lstPedido);

            PedidoController controller = new(_logger.Object, _application.Object, _pagamentoService.Object);
            var result = await controller.Get("","","");

            Assert.NotNull(result);
        }
        [Fact]
        public async Task Get_obterporId_OKAsync()
        {
            var _application = new Mock<IPedidoApplication>();
            var _logger = new Mock<Serilog.ILogger>();
            var _pagamentoService = new Mock<IPagamentoService>();

            _pagamentoService.SetupSequence(x => x.CriarOrdemPagamento(It.IsAny<Pedido>())).ReturnsAsync(true);
            _application.SetupSequence(x => x.ObterPedido(1)).ReturnsAsync(pedido);

            PedidoController controller = new(_logger.Object, _application.Object, _pagamentoService.Object);
            var result = await controller.Get(1);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task POST_OKAsync()
        {
            var _application = new Mock<IPedidoApplication>();
            var _logger = new Mock<Serilog.ILogger>();
            var _pagamentoService = new Mock<IPagamentoService>();

            _pagamentoService.SetupSequence(x => x.CriarOrdemPagamento(It.IsAny<Pedido>())).ReturnsAsync(true);
            _application.SetupSequence(x => x.Inserir(pedido)).ReturnsAsync(pedido);

            PedidoController controller = new(_logger.Object, _application.Object, _pagamentoService.Object);
            var result = await controller.Post(pedidoDTO);

            Assert.NotNull(result);
        }
        [Fact]
        public async Task PUT_OKAsync()
        {
            var _application = new Mock<IPedidoApplication>();
            var _logger = new Mock<Serilog.ILogger>();
            var _pagamentoService = new Mock<IPagamentoService>();

            _pagamentoService.SetupSequence(x => x.CriarOrdemPagamento(It.IsAny<Pedido>())).ReturnsAsync(true);
            _application.SetupSequence(x => x.Atualizar(pedido)).ReturnsAsync(true);

            PedidoController controller = new(_logger.Object, _application.Object, _pagamentoService.Object);
            var result = await controller.Put(pedidoDTO);

            Assert.NotNull(result);
        }
        [Fact]
        public async Task AtualizarStatus_OKAsync()
        {
            var _application = new Mock<IPedidoApplication>();
            var _logger = new Mock<Serilog.ILogger>();
            var _pagamentoService = new Mock<IPagamentoService>();

            _pagamentoService.SetupSequence(x => x.CriarOrdemPagamento(It.IsAny<Pedido>())).ReturnsAsync(true);
            _application.SetupSequence(x => x.AtualizarStatus(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            PedidoController controller = new(_logger.Object, _application.Object, _pagamentoService.Object);
            var result = await controller.AtualizarStatus("","","");

            Assert.NotNull(result);
        }
    }
}
