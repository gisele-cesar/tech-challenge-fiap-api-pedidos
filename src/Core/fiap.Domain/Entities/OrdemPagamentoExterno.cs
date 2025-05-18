﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fiap.Domain.Entities
{
    public class OrdemPagamentoExterno
    {
        public string external_reference { get; set; }
        public string notification_url { get; set; }
        public string expiration_date { get; set; }
        public decimal total_amount { get; set; }
        public List<Item> items { get; set; }
        public string title { get; set; }
        public string description { get; set; }
    }
    public class Item
    {
        public string sku_number { get; set; }
        public string category { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public int quantity { get { return 1; } }
        public string unit_measure { get { return "unit"; } }
        public decimal unit_price { get; set; }
        public decimal total_amount { get; set; }
    }
}
