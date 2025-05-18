﻿using Amazon.DynamoDBv2;
using fiap.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace fiap.Repositories
{
    public static class RepositoriesModuleDependency
    {
        public static void AddRepositoriesModule(this IServiceCollection services)
        {
            services.AddAWSService<IAmazonDynamoDB>();
            services.AddScoped<IPedidoRepository, PedidoRepository>();
        }
    }
}
