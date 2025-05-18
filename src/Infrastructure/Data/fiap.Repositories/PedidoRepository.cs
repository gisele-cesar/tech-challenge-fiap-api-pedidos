using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using fiap.Domain.Entities;
using fiap.Domain.Interfaces;
using Serilog;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace fiap.Repositories
{
    public class PedidoRepository : IPedidoRepository
    {
        private readonly ILogger _logger;
        private readonly Func<IDbConnection> _connectionFactory;
        private readonly IItemPedidoRepository _itemPedidoRepository;
        private readonly IAmazonDynamoDB _amazonDynamoDb;
        private const string FIAP_PEDIDO_DYNAMODB = "fiap-pedido";

        public PedidoRepository(ILogger logger, Func<IDbConnection> connectionFactory, IItemPedidoRepository itemPedidoRepository, IAmazonDynamoDB amazonDynamoDb)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
            _itemPedidoRepository = itemPedidoRepository;
            _amazonDynamoDb = amazonDynamoDb;
        }

        public async Task<List<Pedido>> ObterPedidos()
        {
            var lst = new List<Pedido>();
            try
            {
                var queryRequest = new QueryRequest
                {
                    TableName = FIAP_PEDIDO_DYNAMODB
                };
                var queryResponse = await _amazonDynamoDb.QueryAsync(queryRequest);

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

                    lst.Add(new Pedido
                    {
                        IdPedido = item["IdPedido"].ToString(),
                        Cliente = new Cliente
                        {
                            Cpf = item["cliente.Cpf"].ToString(),
                            Email = item["cliente.Email"].ToString(),
                            Id = Convert.ToInt32(item["cliente.Id"]),
                            Nome = item["cliente.Nome"].ToString()
                        },
                        Numero = item["NumeroPedido"].ToString(),
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
                string tableName = "fiap-pedido";
                string gsiName = "StatusPedido-index";
                var statusList = new[] { status1, status2, status3 };
                var pedidos = new List<Pedido>();

                foreach (var status in statusList)
                {
                    var queryRequest = new QueryRequest
                    {
                        TableName = tableName,
                        IndexName = gsiName,
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
                            Produtos = produtos
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
                using var connection = _connectionFactory();
                connection.Open();
                _logger.Information("Conexão com o banco de dados realizada com sucesso!");

                using var command = connection.CreateCommand();
                command.CommandText = @"
                                SELECT p.*, sp.Descricao AS DescricaoStatusPedido, pg.Descricao AS DescricaoStatusPagamento 
                                FROM Pedido p 
                                JOIN StatusPedido sp ON p.IdStatusPedido = sp.IdStatusPedido
                                JOIN StatusPagamento pg ON p.IdStatusPagamento = pg.IdStatusPagamento
                                WHERE IdPedido = @id";
                var param = command.CreateParameter();
                param.ParameterName = "@id";
                param.Value = idPedido;
                command.Parameters.Add(param);
                Pedido pedido = null;
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    pedido = new Pedido
                    {
                        IdPedido = idPedido,
                        IdCliente = (int)reader["IdCliente"],
                        Numero = reader["NumeroPedido"].ToString(),
                        StatusPedido = new StatusPedido
                        {
                            IdStatusPedido = (int)reader["IdStatusPedido"],
                            Descricao = reader["DescricaoStatusPedido"].ToString()
                        },
                        StatusPagamento = new StatusPagamento
                        {
                            IdStatusPagamento = (int)reader["IdStatusPagamento"],
                            Descricao = reader["DescricaoStatusPagamento"].ToString()
                        },

                        Produtos = await _itemPedidoRepository.ObterItemPedido(idPedido)
                    };
                    _logger.Information($"Pedido id: {idPedido} obtido com sucesso!");
                    return pedido;
                }
                else
                {
                    throw new Exception($"Id pedido {idPedido} não encontrado.");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Erro ao obter pedido id {idPedido}. Erro: {ex.Message}.");
                throw;
            }
        }

        public Task<Pedido> Inserir(Pedido pedido)
        {
            string tableName = "fiap-pedido";

            var request = new PutItemRequest
            {
                TableName = tableName,

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

            _amazonDynamoDb.PutItemAsync(request).Wait();

            return Task.FromResult(pedido);
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
                        { "StatusPedido", new AttributeValueUpdate {Action = AttributeAction.PUT, Value = new AttributeValue { S = pedido.StatusPedido.ToString() } },
                        { "StatusPagamento", new AttributeValueUpdate { Action = AttributeAction.PUT, Value = new AttributeValue { S = pedido.StatusPagamento.ToString() } },
                        { "ValorTotal", new AttributeValueUpdate { Action = AttributeAction.PUT, Value = new AttributeValue { N = pedido.ValorTotal.ToString() } },
                        { "DataAlteracao", new AttributeValueUpdate {Action = AttributeAction.PUT, Value = new AttributeValue {  S = DateTime.UtcNow.ToString("o") } }
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
    }
}


