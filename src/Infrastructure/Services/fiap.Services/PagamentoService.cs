using fiap.Domain.Entities;
using fiap.Domain.Interfaces;
using Serilog;
using System.Text;
using System.Text.Json;

namespace fiap.Services
{
    public class PagamentoService : IPagamentoService
    {
        private readonly IHttpClientFactory _httpClient;
        private readonly ILogger _logger;

        public PagamentoService(IHttpClientFactory httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }
        public async Task<bool> CriarOrdemPagamento(Pedido pedido)
        {
            var jsonContent = JsonSerializer.Serialize(pedido);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var client = _httpClient.CreateClient("pagamentoService");
            /// client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token...");

            var response = await client.PostAsync("http://aac267167420245da82de89ad7566193-1161998588.us-east-1.elb.amazonaws.com/api/Pagamento/CriarOrdemPagamento", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.Information($"Requisicao de criacao de ordem de pagamento parao pedido id: {pedido.IdPedido} no servico de pagamentos realizada com sucesso! Content: {response.Content.ReadAsStringAsync()}");
                return true;
            }

            _logger.Error($"Erro ao tentar criar ordem de pagamento no MP. Content: {response.Content.ReadAsStream()}");
            return false;
        }
    }
}
