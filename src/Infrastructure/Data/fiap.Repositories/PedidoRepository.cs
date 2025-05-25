using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using fiap.Domain.Entities;
using fiap.Domain.Interfaces;
using Serilog;
using System.Data;

namespace fiap.Repositories
{
    public class PedidoRepository : IPedidoRepository
    {
        private readonly ILogger _logger;
        private readonly IAmazonDynamoDB _amazonDynamoDb;
        private const string FIAP_PEDIDO_DYNAMODB = "fiap-pedido";

        public PedidoRepository(ILogger logger, IAmazonDynamoDB amazonDynamoDb)
        {
            _logger = logger;
            _amazonDynamoDb = amazonDynamoDb;
        }

        public async Task<List<Pedido>> ObterPedidos()
        {
            var lst = new List<Pedido>();
            try
            {
                var scanRequest = new ScanRequest
                {
                    TableName = FIAP_PEDIDO_DYNAMODB
                };
                var queryResponse = await _amazonDynamoDb.ScanAsync(scanRequest);

                foreach (var item in queryResponse.Items)
                {
                    var produtos = new List<Produto>();
                    if (item.TryGetValue("Produtos", out AttributeValue value) && value.L != null)
                    {
                        foreach (var prodAttr in value.L)
                        {
                            var prodMap = prodAttr.M;
                            produtos.Add(new Produto
                            {
                                IdProduto = int.Parse(prodMap["IdProduto"].N),
                                Nome = prodMap["Nome"].S,
                                Preco = decimal.Parse(prodMap["Preco"].N)
                            });
                        }
                    }

                    // Map Cliente
                    var clienteMap = item.ContainsKey("Cliente") ? item["Cliente"].M : null;
                    var cliente = clienteMap != null
                        ? new Cliente
                        {
                            Id = int.Parse(clienteMap["IdCliente"].N),
                            Nome = clienteMap["Nome"].S,
                            Email = clienteMap["Email"].S,
                            Cpf = clienteMap["Cpf"].S
                        }
                        : null;


                    lst.Add(new Pedido
                    {
                        IdPedido = item["IdPedido"].S,
                        Cliente = cliente,
                        Numero = item["NumeroPedido"].S,
                        StatusPedido = Enum.Parse<StatusPedido>(item["StatusPedido"].S),
                        StatusPagamento = Enum.Parse<StatusPagamento>(item["StatusPagamento"].S),
                        Produtos = produtos,
                    });
                }

                _logger.Information("Lista de pedidos obtida com sucesso!");

                return await Task.FromResult(lst);
            }
            catch (Exception ex)
            {
                _logger.Error($"Erro ao obter pedidos. Erro: {ex.Message}.");
                throw;
            }
        }

        public async Task<List<Pedido>> ObterPedidosPorStatus(string status1, string status2, string status3)
        {
            try
            {
                var statusList = new[] { status1, status2, status3 };
                var pedidos = new List<Pedido>();

                foreach (var status in statusList)
                {
                    var queryRequest = new QueryRequest
                    {
                        TableName = FIAP_PEDIDO_DYNAMODB,
                        IndexName = "StatusPedido-index",
                        KeyConditionExpression = "StatusPedido = :status",
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                        {
                            { ":status", new AttributeValue { S = status } }
                        }
                    };

                    var response = await _amazonDynamoDb.QueryAsync(queryRequest);

                    foreach (var item in response.Items)
                    {
                        // Exclude "Solicitado" and "Finalizado"
                        if (item["StatusPedido"].S == "Solicitado" || item["StatusPedido"].S == "Finalizado")
                            continue;

                        // Map Cliente
                        var clienteMap = item.ContainsKey("Cliente") ? item["Cliente"].M : null;
                        var cliente = clienteMap != null
                            ? new Cliente
                            {
                                Id = int.Parse(clienteMap["IdCliente"].N),
                                Nome = clienteMap["Nome"].S,
                                Email = clienteMap["Email"].S,
                                Cpf = clienteMap["Cpf"].S
                            }
                            : null;

                        // Map Produtos (list)
                        var produtos = new List<Produto>();
                        if (item.ContainsKey("Produtos") && item["Produtos"].L != null)
                        {
                            foreach (var prodAttr in item["Produtos"].L)
                            {
                                var prodMap = prodAttr.M;
                                produtos.Add(new Produto
                                {
                                    IdProduto = int.Parse(prodMap["IdProduto"].N),
                                    Nome = prodMap["Nome"].S,
                                    Preco = decimal.Parse(prodMap["Preco"].N)
                                });
                            }
                        }

                        pedidos.Add(new Pedido
                        {
                            IdPedido = item["IdPedido"].S,
                            Cliente = cliente,
                            Numero = item["NumeroPedido"].S,
                            StatusPedido = Enum.Parse<StatusPedido>(item["StatusPedido"].S),
                            StatusPagamento = Enum.Parse<StatusPagamento>(item["StatusPagamento"].S),
                            Produtos = produtos,
                            DataCriacao = item["DataCriacao"].S
                        });
                    }
                }

                // Fix: Replace 'item' with 'p' in the ordering logic
                pedidos = pedidos
                    .OrderBy(p => Array.IndexOf(statusList, p.StatusPedido.ToString()))
                    .ThenBy(p => DateTime.Parse(p.DataCriacao))
                    .ToList();

                _logger.Information("Lista de pedidos ordenada por status com sucesso!");
                return pedidos;
            }
            catch (Exception ex)
            {
                _logger.Error($"Erro ao obter pedidos por status. Erro: {ex.Message}.");
                throw;
            }
        }

        public async Task<Pedido> ObterPedido(int idPedido)
        {
            try
            {
                var queryRequest = new GetItemRequest
                {
                    TableName = FIAP_PEDIDO_DYNAMODB,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        { "IdPedido", new AttributeValue { S = idPedido.ToString() } }
                    }
                };

                var response = await _amazonDynamoDb.GetItemAsync(queryRequest);

                if (response.Item == null || response.Item.Count == 0)
                    throw new Exception($"Id pedido {idPedido} não encontrado.");

                var item = response.Item;

                // Map Cliente
                var clienteMap = item.ContainsKey("Cliente") ? item["Cliente"].M : null;
                var cliente = clienteMap != null
                    ? new Cliente
                    {
                        Id = int.Parse(clienteMap["IdCliente"].N),
                        Nome = clienteMap["Nome"].S,
                        Email = clienteMap["Email"].S,
                        Cpf = clienteMap["Cpf"].S
                    }
                    : null;

                // Map Produtos (list)
                var produtos = new List<Produto>();
                if (item.ContainsKey("Produtos") && item["Produtos"].L != null)
                {
                    foreach (var prodAttr in item["Produtos"].L)
                    {
                        var prodMap = prodAttr.M;
                        produtos.Add(new Produto
                        {
                            IdProduto = int.Parse(prodMap["IdProduto"].N),
                            Nome = prodMap["Nome"].S,
                            Preco = decimal.Parse(prodMap["Preco"].N)
                        });
                    }
                }

                var pedido = new Pedido
                {
                    IdPedido = item["IdPedido"].S,
                    Cliente = cliente,
                    Numero = item["NumeroPedido"].S,
                    StatusPedido = Enum.Parse<StatusPedido>(item["StatusPedido"].S),
                    StatusPagamento = Enum.Parse<StatusPagamento>(item["StatusPagamento"].S),
                    Produtos = produtos,
                    DataCriacao = item.ContainsKey("DataCriacao") ? item["DataCriacao"].S : null,
                    DataAlteracao = item.ContainsKey("DataAlteracao") ? item["DataAlteracao"].S : null,
                };

                _logger.Information($"Pedido id: {idPedido} obtido com sucesso!");
                return pedido;
            }
            catch (Exception ex)
            {
                _logger.Error($"Erro ao obter pedido id {idPedido}. Erro: {ex.Message}.");
                throw;
            }
        }
        public Task<Pedido> Inserir(Pedido pedido)
        {
            try
            {
                var queryRequest = new PutItemRequest
                {
                    TableName = FIAP_PEDIDO_DYNAMODB,

                    Item = new Dictionary<string, AttributeValue>
                {
                    { "IdPedido", new AttributeValue { S = pedido.IdPedido } },
                    { "Cliente", new AttributeValue { M = new Dictionary<string, AttributeValue>
                        {
                            { "IdCliente", new AttributeValue { N = pedido.Cliente.Id.ToString() } },
                            { "Nome", new AttributeValue { S = pedido.Cliente.Nome } },
                            { "Email", new AttributeValue { S = pedido.Cliente.Email } },
                            { "Cpf", new AttributeValue { S = pedido.Cliente.Cpf } }
                        }
                    }},
                    { "Produtos", new AttributeValue { L = new List<AttributeValue>
                        {
                            new AttributeValue { M = new Dictionary<string, AttributeValue>
                                {
                                    { "IdProduto", new AttributeValue { N = pedido.Produtos.FirstOrDefault().IdProduto.ToString() } },
                                    { "Nome", new AttributeValue { S = pedido.Produtos.FirstOrDefault().Nome } },
                                    { "Preco", new AttributeValue { N = pedido.Produtos.FirstOrDefault().Preco.ToString() } }
                                }
                            }
                        }
                    }},
                    { "NumeroPedido", new AttributeValue { S = pedido.Numero } },
                    { "StatusPedido", new AttributeValue { S = pedido.StatusPedido.ToString() } },
                    { "StatusPagamento", new AttributeValue { S = pedido.StatusPagamento.ToString() } },
                    { "ValorTotal", new AttributeValue { N = pedido.ValorTotal.ToString() } },
                    { "DataCriacao", new AttributeValue { S = DateTime.UtcNow.ToString("o") } },
                    { "DataAlteracao", new AttributeValue { S = DateTime.UtcNow.ToString("o") } }
                }
                };

                _amazonDynamoDb.PutItemAsync(queryRequest).Wait();

                _logger.Information($"Pedido número {pedido.Numero} inserido com sucesso! Pedido id: {pedido.IdPedido}.");
                return Task.FromResult(pedido);

            }
            catch (Exception ex)
            {
                _logger.Error($"Erro ao incluir novo pedido número {pedido.Numero}. Erro: {ex.Message}.");
                throw;
            }

        }

        public Task<bool> Atualizar(Pedido pedido)
        {
            try
            {
                string tableName = "fiap-pedido";

                var request = new UpdateItemRequest
                {
                    TableName = tableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        { "idPedido", new AttributeValue { S = pedido.IdPedido } }
                    },
                    AttributeUpdates = new Dictionary<string, AttributeValueUpdate>
                    {
                        { "StatusPedido", new AttributeValueUpdate {Action = AttributeAction.PUT, Value = new AttributeValue { S = pedido.StatusPedido.ToString() } } },
                        { "StatusPagamento", new AttributeValueUpdate { Action = AttributeAction.PUT, Value = new AttributeValue { S = pedido.StatusPagamento.ToString() } } },
                        { "ValorTotal", new AttributeValueUpdate { Action = AttributeAction.PUT, Value = new AttributeValue { N = pedido.ValorTotal.ToString() } } },
                        { "DataAlteracao", new AttributeValueUpdate {Action = AttributeAction.PUT, Value = new AttributeValue {  S = DateTime.UtcNow.ToString("o") } } }
                    }
                };

                _amazonDynamoDb.UpdateItemAsync(request).Wait();

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.Error($"Erro ao atualizar pedido id: {pedido.IdPedido}. Erro: {ex.Message}");
                throw;
            }
        }

        public Task<bool> AtualizarStatus(string idPedido, string statusPedido, string statusPagamento)
        {
            try
            {
                string tableName = "fiap-pedido";

                var request = new UpdateItemRequest
                {
                    TableName = tableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        { "IdPedido", new AttributeValue { S = idPedido } }
                    },
                    AttributeUpdates = new Dictionary<string, AttributeValueUpdate>
                    {
                        { "StatusPedido", new AttributeValueUpdate {Action = AttributeAction.PUT, Value = new AttributeValue { S = statusPedido } } },
                        { "StatusPagamento", new AttributeValueUpdate { Action = AttributeAction.PUT, Value = new AttributeValue { S = statusPagamento } } },
                        { "DataAlteracao", new AttributeValueUpdate {Action = AttributeAction.PUT, Value = new AttributeValue {  S = DateTime.UtcNow.ToString("o") } } }
                    }
                };

                _amazonDynamoDb.UpdateItemAsync(request).Wait();

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.Error($"Erro ao atualizar pedido id: {idPedido}. Erro: {ex.Message}");
                throw;
            }
        }
    }
}


