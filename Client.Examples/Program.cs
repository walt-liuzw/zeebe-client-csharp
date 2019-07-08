//
//    Copyright (c) 2018 camunda services GmbH (info@camunda.com)
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Zeebe.Client;
using Zeebe.Client.Api.Clients;
using Zeebe.Client.Api.Responses;

namespace Client.Examples
{
    internal class Program
    {
        private static readonly string DemoProcessPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "demo-process.bpmn");
        private static readonly string ZeebeUrl = "127.0.0.1:26500";
        private static readonly long WORK_COUNT = 2L;

        //private static string WorkflowInstanceVariables =
        //    "{\"orderId\":\"e895b984-8677-4ff2-be7a-037e253f1e08\",\"amount_instance\":\"30\",\"func_instance\":\"collect money for your orders\"}";
        private static IZeebeClient _client;

        public static async Task Main(string[] args)
        {
            // create zeebe client
            _client = ZeebeClient.NewZeebeClient(ZeebeUrl);
            // deploy
            var deployResponse = await _client.NewDeployCommand().AddResourceFile(DemoProcessPath).Send();
            // create workflow instance
            var workflowKey = deployResponse.Workflows[0].WorkflowKey;

            // 客户端生成多个流程数据
            await GenerateMultiFlowInstances(workflowKey);
            //await _client.NewSetVariablesCommand(workflowInstance.WorkflowInstanceKey).Variables("{\"func_instance\":\"collect money for your orders\"}").Local().Send();
            
            // open job worker
            using (var signal = new EventWaitHandle(false, EventResetMode.AutoReset))
            {
                _client.NewWorker()
                    .JobType("collectmoney")
                    .Handler(HandleJob)
                    .MaxJobsActive(5)
                    .Name("collectmoney")
                    .AutoCompletion()
                    .PollInterval(TimeSpan.FromSeconds(1))
                    .Timeout(TimeSpan.FromSeconds(10))
                    .Open();
                _client.NewWorker()
                    .JobType("fetchitems")
                    .Handler(HandleJob)
                    .MaxJobsActive(5)
                    .Name("fetchitems")
                    .AutoCompletion()
                    .PollInterval(TimeSpan.FromSeconds(1))
                    .Timeout(TimeSpan.FromSeconds(10))
                    .Open();
                _client.NewWorker()
                    .JobType("shipparcel")
                    .Handler(HandleJob)
                    .MaxJobsActive(5)
                    .Name("shipparcel")
                    .AutoCompletion()
                    .PollInterval(TimeSpan.FromSeconds(1))
                    .Timeout(TimeSpan.FromSeconds(10))
                    .Open();
                // blocks main thread, so that worker can run
                WaitHandle.WaitAll(new WaitHandle[]
                {
                    signal
                });
            }

        }

        private static  async Task GenerateMultiFlowInstances(long workflowKey)
        {
            for (var i = 0; i < WORK_COUNT; i++)
            {
                var variables = new
                {
                    orderId = $"{DateTime.Now.Ticks}-{i}",
                    amount_instance = 20+i,
                    func_instance = "collect money for your orders"
                };
                await _client
                    .NewCreateWorkflowInstanceCommand()
                   
                    .WorkflowKey(workflowKey)
                    .Variables(JsonConvert.SerializeObject(variables))
                    .Send();
            }
        }

        private static void HandleJob(IJobClient jobClient, IJob job)
        {
            // business logic
            var variables = JsonConvert.DeserializeObject<Dictionary<string, object>>(job.Variables);
            switch (job.Type)
            {
                case "collectmoney":
                    string message = $"you have a order to pay! amount_job:{variables["amount_job"]}";
                    Console.WriteLine($"OrderId:{variables["orderId"]},Function:{variables["func_job"]},message:{message}");
                    var variablesObject = new
                    {
                        func_job= "handle stock and prepare delivery",
                        amount_job = Convert.ToInt32(variables["amount_job"])+50
                    };
                    jobClient.NewCompleteJobCommand(job).Variables(JsonConvert.SerializeObject(variablesObject)).Send();
                    break;
                case "fetchitems":
                    message = $"you should prepare cargoes! amount updated is:{variables["amount_job"]}";
                    Console.WriteLine($"OrderId:{variables["orderId"]},Function:{variables["func_job"]},message:{message}");
                    jobClient.NewCompleteJobCommand(job).Variables("{\"func_job\":\"handle delivery and so on\"}").Send();
                    break;
                case "shipparcel":
                    Console.WriteLine($"OrderId:{variables["orderId"]},Function:{variables["func_job"]}");
                    jobClient.NewCompleteJobCommand(job).Send();
                    break;
                default:
                    Console.WriteLine($"OrderId:{variables["orderId"]},Function:default");
                    break;
            }

        }
    }
}
