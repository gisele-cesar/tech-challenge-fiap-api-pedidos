using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime.Internal.Transform;
using fiap.Domain.Entities;
using fiap.Domain.Interfaces;
using Serilog;
using Serilog.Core;
using System.Data;
using System.Data.Common;
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

        public PedidoRepository(ILogger logger, Func<IDbConnection> connectionFactory, IItemPedidoRepository itemPedidoRepository, IAmazonDynamoDB amazonDynamoDb)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
            _itemPedidoRepository = itemPedidoRepository;
            _amazonDynamoDb = amazonDynamoDb;
        }

        public async Task<List<Pedido>> ObterPedidos()
        {
            try
            {
                using var connection = _connectionFactory();
                connection.Open();
                _logger.Information("Conexão com o banco de dados realizada com sucesso!");

                var lst = new List<Pedido>();
                using var command = connection.CreateCommand();
                command.CommandText = @"
                                SELECT p.*, sp.Descricao AS DescricaoStatusPedido, pg.Descricao AS DescricaoStatusPagamento 
                                FROM Pedido p 
                                JOIN StatusPedido sp ON p.IdStatusPedido = sp.IdStatusPedido
                                JOIN StatusPagamento pg ON p.IdStatusPagamento = pg.IdStatusPagamento";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    lst.Add(new Pedido
                    {
                        IdPedido = (int)reader["IdPedido"],
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
                        Produtos = await _itemPedidoRepository.ObterItemPedido((int)reader["IdPedido"])
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
                using var connection = _connectionFactory();
                connection.Open();
                _logger.Information("Conexão com o banco de dados realizada com sucesso!");

                var lst = new List<Pedido>();
                using var command = connection.CreateCommand();
                command.CommandText = $@"
                                  SELECT p.*, sp.Descricao AS DescricaoStatusPedido, pg.Descricao AS DescricaoStatusPagamento 
                                    FROM Pedido p 
                                    JOIN StatusPedido sp ON p.IdStatusPedido = sp.IdStatusPedido
                                    JOIN StatusPagamento pg ON p.IdStatusPagamento = pg.IdStatusPagamento
                                    WHERE sp.Descricao NOT IN ('Solicitado', 'Finalizado')
                                    ORDER BY 
                                        CASE 
                                            WHEN sp.Descricao = '{status1}' THEN 1
                                            WHEN sp.Descricao = '{status2}' THEN 2
                                            WHEN sp.Descricao = '{status3}' THEN 3
                                            ELSE 4
                                        END,
                                        p.DataCriacao ASC";

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var idPedido = (int)reader["IdPedido"];
                    lst.Add(new Pedido
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
                    });
                }

                _logger.Information("Lista de pedidos ordenada por status com sucesso!");
                return await Task.FromResult(lst);
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

        public Task<bool> AtualizarStatusPedido(Pedido pedido)
        {
            using var connection = _connectionFactory();
            connection.Open();
            _logger.Information("Conexão com o banco de dados realizada com sucesso!");
            using (var transaction = connection.BeginTransaction())
            {
                using var command = connection.CreateCommand();
                try
                {
                    StringBuilder sb = new StringBuilder();
                    command.Transaction = transaction;
                    sb.Append("update Pedido set IdStatusPedido = @idStatusPedido, ");
                    sb.Append("ValorTotalPedido = @valorTotalPedido, DataAlteracao = getdate(), ");
                    sb.Append("IdStatusPagamento = @idStatusPagamento ");
                    sb.Append("where IdPedido = @idPedido");
                    command.CommandText = sb.ToString();

                    command.Parameters.Add(new SqlParameter { ParameterName = "@idPedido", Value = pedido.IdPedido, SqlDbType = SqlDbType.Int });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@idStatusPedido", Value = pedido.StatusPedido.IdStatusPedido, SqlDbType = SqlDbType.Int });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@idStatusPagamento", Value = pedido.StatusPagamento.IdStatusPagamento, SqlDbType = SqlDbType.Int });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@valorTotalPedido", Value = pedido.ValorTotal, SqlDbType = SqlDbType.Decimal });

                    command.ExecuteNonQuery();

                    transaction.Commit();
                    _logger.Information($"Status pedido id: {pedido.IdPedido} atualizado com sucesso!");
                    return Task.FromResult(command.ExecuteNonQuery() >= 1);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Erro ao atualizar pedido id: {pedido.IdPedido}. Erro: {ex.Message}");
                    transaction.Rollback();
                    throw;
                }
            }
        }
        public Task<bool> Atualizar(Pedido pedido)
        {
            using var connection = _connectionFactory();
            connection.Open();
            _logger.Information("Conexão com o banco de dados realizada com sucesso!");
            using (var transaction = connection.BeginTransaction())
            {
                using var command = connection.CreateCommand();
                using var commandDeletar = command;
                using var command2 = command;
                try
                {
                    StringBuilder sb = new StringBuilder();
                    command.Transaction = transaction;
                    sb.Append("update Pedido set IdStatusPedido = @idStatusPedido, ");
                    sb.Append("ValorTotalPedido = @valorTotalPedido, DataAlteracao = getdate(), ");
                    sb.Append("IdStatusPagamento = @idStatusPagamento ");
                    sb.Append("where IdPedido = @idPedido");
                    command.CommandText = sb.ToString();

                    command.Parameters.Add(new SqlParameter { ParameterName = "@idPedido", Value = pedido.IdPedido, SqlDbType = SqlDbType.Int });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@idStatusPedido", Value = pedido.StatusPedido.IdStatusPedido, SqlDbType = SqlDbType.Int });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@idStatusPagamento", Value = pedido.StatusPagamento.IdStatusPagamento, SqlDbType = SqlDbType.Int });
                    command.Parameters.Add(new SqlParameter { ParameterName = "@valorTotalPedido", Value = pedido.ValorTotal, SqlDbType = SqlDbType.Decimal });

                    command.ExecuteNonQuery();

                    
                    commandDeletar.Transaction = transaction;
                    commandDeletar.CommandText = "delete ItemPedido where idPedido = @idPedido";

                    commandDeletar.Parameters.Add(new SqlParameter { ParameterName = "@idPedido", Value = pedido.IdPedido, SqlDbType = SqlDbType.Int });

                    commandDeletar.ExecuteNonQuery();

                    foreach (var item in pedido.Produtos)
                    {
                        
                        command2.Transaction = transaction;
                        command2.CommandText = "insert ItemPedido values(@idPedido, @idProduto)";

                        command2.Parameters.Add(new SqlParameter { ParameterName = "@idPedido", Value = pedido.IdPedido, SqlDbType = SqlDbType.Int });
                        command2.Parameters.Add(new SqlParameter { ParameterName = "@idProduto", Value = item.IdProduto, SqlDbType = SqlDbType.Int });

                        command2.ExecuteNonQuery();
                    }
                    transaction.Commit();
                    _logger.Information($"Pedido id: {pedido.IdPedido} atualizado com sucesso!");
                    return Task.FromResult(command.ExecuteNonQuery() >= 1);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Erro ao atualizar pedido id: {pedido.IdPedido}. Erro: {ex.Message}");
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }

}
