using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using fiap.Domain.Entities;
using fiap.Domain.Interfaces;
using fiap.Repositories;
using Moq;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Xunit;

namespace fiap.Tests.Repositories
{
    public class PedidoRepositoryTests
    {
        private readonly Pedido pedido;
        private readonly List<Pedido> lstPedido = [];
        public PedidoRepositoryTests()
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
        public void ObterPedidosTest()
        {
            var _mockDynamoDb = new Mock<IAmazonDynamoDB>();
            var _logger = new Mock<Serilog.ILogger>();

            _mockDynamoDb.Setup(m => m.ScanAsync(It.IsAny<ScanRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScanResponse
            {
                Items = new List<Dictionary<string, AttributeValue>>
                {
                    new Dictionary<string, AttributeValue>
                    {
                        { "IdPedido", new AttributeValue { S = "001" } },
                        { "NumeroPedido", new AttributeValue { S = "12345" } },
                        { "StatusPedido", new AttributeValue { S = "EmPreparacao" } },
                        { "StatusPagamento", new AttributeValue { S = "Aprovado" } },
                        { "Cliente", new AttributeValue { M = new Dictionary<string, AttributeValue>
                            {
                                { "IdCliente", new AttributeValue { N = "1" } },
                                { "Nome", new AttributeValue { S = "Leandro" } },
                                { "Email", new AttributeValue { S = "leandro@email.com" } },
                                { "Cpf", new AttributeValue { S = "12345678900" } }
                            }
                        }},
                        { "Produtos", new AttributeValue { L = new List<AttributeValue>
                            {
                                new AttributeValue { M = new Dictionary<string, AttributeValue>
                                    {
                                        { "IdProduto", new AttributeValue { N = "1" } },
                                        { "Nome", new AttributeValue { S = "Produto A" } },
                                        { "Preco", new AttributeValue { N = "50.25" } }
                                    }
                                }
                            }
                        }}
                    }
                }
            });

            var request = new GetItemRequest
            {
                TableName = "MinhaTabela",
                Key = new Dictionary<string, AttributeValue>
                {
                    { "idPedido", new AttributeValue { S = "123" } }
                }
            };


            var data = new PedidoRepository(_logger.Object, _mockDynamoDb.Object);

            //Act
            var result = data.ObterPedidos();

            Assert.NotNull(result);
        }

        [Fact]
        public async void ObterPedidosTest_exception()
        {
            var _mockDynamoDb = new Mock<IAmazonDynamoDB>();
            var _logger = new Mock<Serilog.ILogger>();

            _mockDynamoDb.Setup(m => m.ScanAsync(It.IsAny<ScanRequest>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new Exception("Erro ao Obter Pedidos"));

            var data = new PedidoRepository(_logger.Object, _mockDynamoDb.Object);

            var ex = await Assert.ThrowsAsync<Exception>(() => data.ObterPedidos());

            Assert.Equal("Erro ao Obter Pedidos", ex.Message);
        }

        [Fact]
        public void ObterPedidoPorStatusTest()
        {
            var _mockDynamoDb = new Mock<IAmazonDynamoDB>();
            var _logger = new Mock<Serilog.ILogger>();

            var request = new QueryRequest
            {
                TableName = "MinhaTabela",
                KeyConditionExpression = "StatusPedido = :status",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":id", new AttributeValue { S = "123" } }
                }
            };

            _mockDynamoDb
           .SetupSequence(m => m.QueryAsync(request, It.IsAny<CancellationToken>()))
           .ReturnsAsync(new QueryResponse
           {
               Items = new List<Dictionary<string, AttributeValue>>
               {
                    new Dictionary<string, AttributeValue>
                    {
                        { "id", new AttributeValue { S = "123" } },
                        { "nome", new AttributeValue { S = "Leandro" } },
                        { "idade", new AttributeValue { N = "30" } }
                    },
                    new Dictionary<string, AttributeValue>
                    {
                        { "id", new AttributeValue { S = "124" } },
                        { "nome", new AttributeValue { S = "Ana" } },
                        { "idade", new AttributeValue { N = "25" } }
                    }
               }
           });

            _mockDynamoDb.Setup(m => m.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new QueryResponse
          {
              Items = new List<Dictionary<string, AttributeValue>>
              {
                    new Dictionary<string, AttributeValue>
                    {
                        { "IdPedido", new AttributeValue { S = "001" } },
                        { "StatusPedido", new AttributeValue { S = "Solicitado" } },
                        { "NumeroPedido", new AttributeValue { S = "12345" } },
                        { "StatusPagamento", new AttributeValue { S = "Pago" } }
                    },
                    new Dictionary<string, AttributeValue>
                    {
                        { "IdPedido", new AttributeValue { S = "002" } },
                        { "StatusPedido", new AttributeValue { S = "Solicitado" } },
                        { "NumeroPedido", new AttributeValue { S = "67890" } },
                        { "StatusPagamento", new AttributeValue { S = "Pendente" } }
                    }
              }
          });



            var data = new PedidoRepository(_logger.Object, _mockDynamoDb.Object);

            //Act
            var result = data.ObterPedidosPorStatus("1","2","3");

            Assert.NotNull(result);
        }

        [Fact]
        public async Task ObterPedidosPorStatus_DeveRetornarPedidosFiltrados()
        {
            // Configurando o mock do DynamoDB
            var _mockDynamoDb = new Mock<IAmazonDynamoDB>();
            var _logger = new Mock<Serilog.ILogger>();
            _mockDynamoDb.Setup(m => m.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueryResponse
                {
                    Items = new List<Dictionary<string, AttributeValue>>
                    {
                    new Dictionary<string, AttributeValue>
                    {
                        { "DataCriacao", new AttributeValue { S = DateTime.Now.ToString() } },
                        { "IdPedido", new AttributeValue { S = "001" } },
                        { "StatusPedido", new AttributeValue { S = "EmPreparacao" } },
                        { "NumeroPedido", new AttributeValue { S = "12345" } },
                        { "StatusPagamento", new AttributeValue { S ="Aprovado" } },
                        { "Cliente", new AttributeValue { M = new Dictionary<string, AttributeValue>
                            {
                                { "IdCliente", new AttributeValue { N = "1" } },
                                { "Nome", new AttributeValue { S = "Leandro" } },
                                { "Email", new AttributeValue { S = "leandro@email.com" } },
                                { "Cpf", new AttributeValue { S = "12345678900" } }
                            }
                        }},
                        { "Produtos", new AttributeValue { L = new List<AttributeValue>
                            {
                                new AttributeValue { M = new Dictionary<string, AttributeValue>
                                    {
                                        { "IdProduto", new AttributeValue { N = "1" } },
                                        { "Nome", new AttributeValue { S = "Produto A" } },
                                        { "Preco", new AttributeValue { N = "50.25" } }
                                    }
                                }
                            }
                        }}
                    },
                    new Dictionary<string, AttributeValue>
                    {
                        { "DataCriacao", new AttributeValue { S = DateTime.Now.ToString() } },
                        { "IdPedido", new AttributeValue { S = "002" } },
                        { "StatusPedido", new AttributeValue { S = "Cancelado" } },
                        { "NumeroPedido", new AttributeValue { S = "67890" } },
                        { "StatusPagamento", new AttributeValue { S = "Pendente" } },
                        { "Cliente", new AttributeValue { M = new Dictionary<string, AttributeValue>
                            {
                                { "IdCliente", new AttributeValue { N = "1" } },
                                { "Nome", new AttributeValue { S = "Leandro" } },
                                { "Email", new AttributeValue { S = "leandro@email.com" } },
                                { "Cpf", new AttributeValue { S = "12345678900" } }
                            }
                        }},
                        { "Produtos", new AttributeValue { L = new List<AttributeValue>
                            {
                                new AttributeValue { M = new Dictionary<string, AttributeValue>
                                    {
                                        { "IdProduto", new AttributeValue { N = "1" } },
                                        { "Nome", new AttributeValue { S = "Produto A" } },
                                        { "Preco", new AttributeValue { N = "50.25" } }
                                    }
                                }
                            }
                        }}
                    }
                    }
                });

            var data = new PedidoRepository(_logger.Object, _mockDynamoDb.Object);
            var pedidos = await data.ObterPedidosPorStatus("EmPreparacao", "Pronto", "Finalizado");

            // Verificações
            Assert.NotNull(pedidos);

        }


        [Fact]
        public void ObterPedidoPorIdPedidoTests()
        {
            var _mockDynamoDb = new Mock<IAmazonDynamoDB>();
            var _logger = new Mock<Serilog.ILogger>();

            // Configurando o mock para simular uma resposta de GetItemAsync
            _mockDynamoDb
                .Setup(m => m.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetItemResponse
                {
                    Item = new Dictionary<string, AttributeValue>
                    {
                    { "id", new AttributeValue { S = "123" } },
                    { "nome", new AttributeValue { S = "Leandro" } },
                    { "idade", new AttributeValue { N = "30" } }
                    }
                });

            var request = new GetItemRequest
            {
                TableName = "MinhaTabela",
                Key = new Dictionary<string, AttributeValue>
                {
                    { "idPedido", new AttributeValue { S = "123" } }
                }
            };

            _mockDynamoDb.Setup(m => m.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new QueryResponse
                    {
                      Items = new List<Dictionary<string, AttributeValue>>
                      {
                                        new Dictionary<string, AttributeValue>
                                        {
                                            { "IdPedido", new AttributeValue { S = "001" } },
                                            { "StatusPedido", new AttributeValue { S = "Solicitado" } },
                                            { "NumeroPedido", new AttributeValue { S = "12345" } },
                                            { "StatusPagamento", new AttributeValue { S = "Pago" } }
                                        },
                                        new Dictionary<string, AttributeValue>
                                        {
                                            { "IdPedido", new AttributeValue { S = "002" } },
                                            { "StatusPedido", new AttributeValue { S = "Solicitado" } },
                                            { "NumeroPedido", new AttributeValue { S = "67890" } },
                                            { "StatusPagamento", new AttributeValue { S = "Pendente" } }
                                        }
                      }
                    });

            var data = new PedidoRepository(_logger.Object, _mockDynamoDb.Object);

            //Act
            var result = data.ObterPedido(1);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task Inserir_DeveRetornarPedidoInserido()
        {
            // Configurando o mock do DynamoDB
            var mockDynamoDb = new Mock<IAmazonDynamoDB>();
            var _logger = new Mock<Serilog.ILogger>();
            mockDynamoDb.Setup(m => m.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PutItemResponse());

            var pedido = new Pedido
            {
                IdPedido = "001",
                Cliente = new Cliente { Id = 1, Nome = "Leandro :)", Email = "munarim@email.com", Cpf = "12345678900" },
                Numero = "12345",
                StatusPedido = StatusPedido.EmPreparacao,
                StatusPagamento = StatusPagamento.Aprovado,
                Produtos = new List<Produto>
            {
                new Produto { IdProduto = 1, Nome = "Produto A", Preco = 50.25m },
                 new Produto { IdProduto = 2, Nome = "Produto B", Preco = 50.25m }
            }
            };

            var service = new PedidoRepository(_logger.Object, mockDynamoDb.Object);
            var resultado = await service.Inserir(pedido);

            // Verificações
            Assert.NotNull(resultado);
            Assert.Equal("001", resultado.IdPedido);
            Assert.Equal("Leandro :)", resultado.Cliente.Nome);
            Assert.Equal("12345", resultado.Numero);
            Assert.Equal(StatusPedido.EmPreparacao, resultado.StatusPedido);
            Assert.Equal(StatusPagamento.Aprovado, resultado.StatusPagamento);
            Assert.Equal(100.50m, resultado.ValorTotal);

            mockDynamoDb.Verify(m => m.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }


        [Fact]
        public void Atualizar_Tests()
        {
            var _mockDynamoDb = new Mock<IAmazonDynamoDB>();
            var _logger = new Mock<Serilog.ILogger>();


            // Configurando o mock para simular uma resposta de PutItemAsync
            _mockDynamoDb
                .Setup(m => m.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PutItemResponse());

            var request = new PutItemRequest
            {
                TableName = "MinhaTabela",
                Item = new Dictionary<string, AttributeValue>
            {
                { "id", new AttributeValue { S = "123" } },
                { "nome", new AttributeValue { S = "Leandro" } }
            }
            };


            var data = new PedidoRepository(_logger.Object, _mockDynamoDb.Object);
            //Act
            var result = data.Atualizar(pedido);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task Atualizar_DeveRetornarTrue()
        {
            // Configurando o mock do DynamoDB
            var mockDynamoDb = new Mock<IAmazonDynamoDB>();
            var _logger = new Mock<Serilog.ILogger>();
            mockDynamoDb.Setup(m => m.UpdateItemAsync(It.IsAny<UpdateItemRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UpdateItemResponse());

            var pedido = new Pedido
            {
                IdPedido = "001",
                StatusPedido = StatusPedido.EmPreparacao,
                StatusPagamento = StatusPagamento.Aprovado
            };

            var service = new PedidoRepository(_logger.Object, mockDynamoDb.Object);
            var resultado = await service.Atualizar(pedido);

            // Verificações
            Assert.True(resultado);
            mockDynamoDb.Verify(m => m.UpdateItemAsync(It.IsAny<UpdateItemRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

    }
}