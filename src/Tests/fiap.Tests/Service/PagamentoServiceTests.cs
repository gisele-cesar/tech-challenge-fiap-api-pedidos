using fiap.Domain.Entities;
using fiap.Services;
using Moq;
using Moq.Protected;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace fiap.Tests.Service
{
    public class PagamentoServiceTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<ILogger> _loggerMock;
        private readonly PagamentoService _pagamentoService;

        public PagamentoServiceTests()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _loggerMock = new Mock<ILogger>();

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"RequestId\": \"1\", \"IdTeste\":\"123\"}")
                });

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(_ => _.CreateClient("pagamentoService")).Returns(httpClient);

            _pagamentoService = new PagamentoService(_httpClientFactoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task CriarOrdemPagamento_ShouldReturnTrue_WhenRequestIsSuccessful()
        {
            var pedido = new Pedido { IdPedido = "123" };

            var result = await _pagamentoService.CriarOrdemPagamento(pedido);

            Assert.True(result);
        }

        [Fact]
        public async Task CriarOrdemPagamento_ShouldReturnFalse_WhenRequestFails()
        {
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock.Protected()
                 .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
               .ReturnsAsync(new HttpResponseMessage
               {
                    StatusCode = HttpStatusCode.BadRequest
                });

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(_ => _.CreateClient("pagamentoService")).Returns(httpClient);

            var pedido = new Pedido { IdPedido = "456" };

            var result = await _pagamentoService.CriarOrdemPagamento(pedido);

            Assert.False(result);
        }

    }
}
