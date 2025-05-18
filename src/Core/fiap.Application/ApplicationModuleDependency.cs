﻿using fiap.Application.Interfaces;
using fiap.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace fiap.Application
{
    public static class ApplicationModuleDependency
    {
        public static void AddApplicationModule(this IServiceCollection services)
        {
            services.AddSingleton<IClienteApplication, ClienteApplication>();
            services.AddSingleton<IProdutoApplication, ProdutoApplication>();
            services.AddSingleton<IPedidoApplication, PedidoApplication>();
            services.AddSingleton<IPagamentoApplication, PagamentoApplication>();
        }
    }
}
