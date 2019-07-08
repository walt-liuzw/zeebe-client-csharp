using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using Zeebe.Client;
using Zeebe.Client.Api.Clients;
using Zeebe.Client.Api.Responses;

namespace WarehouseService
{
    public class WarehouseProvider
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static volatile WarehouseProvider _instance;
        private static IZeebeClient _client;

        public static WarehouseProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance=new WarehouseProvider();
                }
                return new WarehouseProvider();
            }
        }
        private WarehouseProvider()
        {
            
        }
       
        public void CollectMoneyHandler(IJobClient client, IJob activatedjob)
        {
            var variables = JsonConvert.DeserializeObject<Dictionary<string, object>>(activatedjob.Variables);
            CollectMoney(variables["orderId"].ToString(), (decimal) variables["amount"]);
            variables["test"] = "test";
        }
        public void FetchItemsHandler(IJobClient client, IJob activatedjob)
        {
            var variables = JsonConvert.DeserializeObject<Dictionary<string, object>>(activatedjob.Variables);
            variables["test"] =  FetchItems(variables["test"].ToString());
        }
        public void ShipHandler(IJobClient client, IJob activatedjob)
        {
            var variables = JsonConvert.DeserializeObject<Dictionary<string, object>>(activatedjob.Variables);
            Ship(variables["test"].ToString());
        }
        /// <summary>
        /// 这个方法代号为CollectMoney（全世界唯一）
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="amount"></param>
        public void CollectMoney(string orderId, decimal amount)
        {
            _logger.Debug($"执行CollectMoney方法------orderId:{orderId},amount:{amount}");
            Console.WriteLine($"执行CollectMoney方法------orderId:{orderId},amount:{amount}");
        }

        /// <summary>
        /// 这个方法代号为FetchItems（全世界唯一）
        /// </summary>
        public string FetchItems(string test)
        {
            _logger.Debug($"执行FetchItems方法------test:{test}");
            Console.WriteLine($"执行FetchItems方法------test:{test}");
            return "test1";
        }
        /// <summary>
        /// 这个方法代号为Ship（全世界唯一）
        /// </summary>
        public void Ship(string test)
        {
            _logger.Debug($"执行Ship方法-----test:{test}");
            Console.WriteLine($"执行Ship方法-----test:{test}");
        }
    }
}
