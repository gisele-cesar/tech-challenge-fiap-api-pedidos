using fiap.API.Controllers;
using fiap.Application.Interfaces;
using fiap.Application.UseCases;
using fiap.Domain.Entities;
using fiap.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace fiap.Tests.Application
{
    public class PedidoApplicationTests
    {
        private readonly Pedido pedido;
        private readonly List<Pedido> lstPedido = [];
        public PedidoApplicationTests()
        {
            pedido = new Pedido
            {
                Cliente = new Cliente { Cpf = "12345678910", Email = "teste@teste.com", Id = 1, Nome = "Joao da Silva" },
                IdPedido = Guid.NewGuid().ToString(),
                Numero = "123456",
                StatusPagamento = StatusPagamento.Pendente,
                StatusPedido = StatusPedido.Solicitado,
                Produtos = [
                     new() { Preco = 1 , Nome = "teste" , IdProduto = 1, IdCategoriaProduto = 1 , Descricao = "teste" , DataCriacao = DateTime.Now , DataAlteracao = DateTime.Now },
                      new() { Preco = 2 , Nome = "teste2" , IdProduto = 2, IdCategoriaProduto = 2 , Descricao = "teste2" , DataCriacao = DateTime.Now , DataAlteracao = DateTime.Now }
                 ]
            };
            lstPedido = [
                pedido,
                pedido
            ];
        }
        [Fact]
        public async Task ObterPedidoPorId_OkAsync()
        {
            var _repo = new Mock<IPedidoRepository>();
            var _logger = new Mock<Serilog.ILogger>();

            _repo.SetupSequence(x => x.ObterPedido(1))
                .ReturnsAsync(pedido);

            PedidoApplication app = new(_logger.Object, _repo.Object);
            var result = await app.ObterPedido(1);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task ObterPedido_DeveLancarExcecao_QuandoRepositorioFalha()
        {
            // Configurando o mock do repositório para lançar uma exceção
            var mockRepositorio = new Mock<IPedidoRepository>();
            var _logger = new Mock<Serilog.ILogger>();
            mockRepositorio.Setup(r => r.ObterPedido(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Erro ao buscar pedido"));

            var service = new PedidoApplication(_logger.Object, mockRepositorio.Object);

            // Verificando se a exceção é lançada corretamente
            var ex = await Assert.ThrowsAsync<Exception>(() => service.ObterPedido(123));

            Assert.Equal("Erro ao buscar pedido", ex.Message);
        }
    

        [Fact]
        public async Task Obter_OkAsync()
        {
            var _repo = new Mock<IPedidoRepository>();
            var _logger = new Mock<Serilog.ILogger>();

            _repo.SetupSequence(x => x.ObterPedidos())
                .ReturnsAsync(lstPedido);

            PedidoApplication app = new(_logger.Object, _repo.Object);
            var result = await app.ObterPedidos();

            Assert.NotNull(result);
        }
        [Fact]
        public async Task Obter_Exception()
        {
            // Configurando o mock do repositório para lançar uma exceção
            var _repo = new Mock<IPedidoRepository>();
            var _logger = new Mock<Serilog.ILogger>();
            _repo.Setup(r => r.ObterPedidos())
                .ThrowsAsync(new Exception("Erro ao buscar pedidos"));

          
            PedidoApplication app = new(_logger.Object, _repo.Object);

            // Verificando se a exceção é lançada corretamente
            var ex = await Assert.ThrowsAsync<Exception>(() => app.ObterPedidos());

            Assert.Equal("Erro ao buscar pedidos", ex.Message);
        }


        [Fact]
        public async Task ObterPedidosPorStatus_OkAsync()
        {
            var _repo = new Mock<IPedidoRepository>();
            var _logger = new Mock<Serilog.ILogger>();

            _repo.SetupSequence(x => x.ObterPedidosPorStatus("1" , "2" , "3"))
                .ReturnsAsync(lstPedido);

            PedidoApplication app = new(_logger.Object, _repo.Object);
            var result = await app.ObterPedidosPorStatus("1", "2", "3");

            Assert.NotNull(result);
        }

        [Fact]
        public async Task ObterPedidosPorStatus_Exception()
        {
            var _repo = new Mock<IPedidoRepository>();
            var _logger = new Mock<Serilog.ILogger>();

            _repo.SetupSequence(x => x.ObterPedidosPorStatus("1", "2", "3"))
                .ThrowsAsync(new Exception("Erro ao buscar pedidos"));

            PedidoApplication app = new(_logger.Object, _repo.Object);
            var ex = await Assert.ThrowsAsync<Exception>(() => app.ObterPedidosPorStatus("1", "2", "3"));

            Assert.Equal("Erro ao buscar pedidos", ex.Message);
        }
        [Fact]
        public async Task Inserir_OkAsync()
        {
            var _repo = new Mock<IPedidoRepository>();
            var _logger = new Mock<Serilog.ILogger>();

            _repo.SetupSequence(x => x.Inserir(pedido))
                .ReturnsAsync(pedido);

            PedidoApplication app = new(_logger.Object, _repo.Object);
            var result = await app.Inserir(pedido);

            Assert.NotNull(result);
        }
        [Fact]
        public async Task Inserir_Exception()
        {
            var _repo = new Mock<IPedidoRepository>();
            var _logger = new Mock<Serilog.ILogger>();

            _repo.SetupSequence(x => x.Inserir(pedido))
                 .ThrowsAsync(new Exception("Erro ao Inserir"));

            PedidoApplication app = new(_logger.Object, _repo.Object);

            var ex = await Assert.ThrowsAsync<Exception>(() => app.Inserir(pedido));

            Assert.Equal("Erro ao Inserir", ex.Message);
        }
        [Fact]
        public async Task Atualizar_OkAsync()
        {
            var _repo = new Mock<IPedidoRepository>();
            var _logger = new Mock<Serilog.ILogger>();

            _repo.SetupSequence(x => x.Atualizar(pedido))
                .ReturnsAsync(true);

            PedidoApplication app = new(_logger.Object, _repo.Object);
            var result = await app.Atualizar(pedido);

            Assert.True(result);
        }
        [Fact]
        public async Task Atualizar_Exception()
        {
            var _repo = new Mock<IPedidoRepository>();
            var _logger = new Mock<Serilog.ILogger>();

            _repo.SetupSequence(x => x.Atualizar(pedido))
                 .ThrowsAsync(new Exception("Erro ao Atualizar"));

            PedidoApplication app = new(_logger.Object, _repo.Object);
            var ex = await Assert.ThrowsAsync<Exception>(() => app.Atualizar(pedido));

            Assert.Equal("Erro ao Atualizar", ex.Message);
        }
        [Fact]
        public async Task AtualizarStatus_OkAsync()
        {
            var _repo = new Mock<IPedidoRepository>();
            var _logger = new Mock<Serilog.ILogger>();

            _repo.SetupSequence(x => x.AtualizarStatus(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            PedidoApplication app = new(_logger.Object, _repo.Object);
            var result = await app.AtualizarStatus("","","");

            Assert.True(result);
        }
        [Fact]
        public async Task AtualizarStatus_Exception()
        {
            var _repo = new Mock<IPedidoRepository>();
            var _logger = new Mock<Serilog.ILogger>();

            _repo.SetupSequence(x => x.AtualizarStatus(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                 .ThrowsAsync(new Exception("Erro ao Atualizar"));

            PedidoApplication app = new(_logger.Object, _repo.Object);
            var ex = await Assert.ThrowsAsync<Exception>(() => app.AtualizarStatus("", "", ""));

            Assert.Equal("Erro ao Atualizar", ex.Message);
        }
    }
}
