using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GatewayProtocol;
using Google.Protobuf;
using NUnit.Framework;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;

namespace Client.UnitTests.Demo
{
    [TestFixture]
    public class OrderPlacedTest : BaseZeebeTest
    {
        
        private readonly string demoProcessPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "order-process.bpmn");

        [Test]
        public void OrderPlacedDemo()
        {
            // 部署BPMN
            DeployBpmnFile().GetAwaiter().GetResult();

            // 实例化
            CreateWorkflowInstance("order-process").GetAwaiter().GetResult();

            // 创建Workers
            CreateWorkers("payment-service", "Collect Money");
            CreateWorkers("inventory-service", "Fetch Items");
            CreateWorkers("shipment-service", "Ship Parcel");

        }

        private void CreateWorkers(string jobType,string jobWorkerName)
        {
            var signal = new EventWaitHandle(false, EventResetMode.AutoReset);
            var receivedJobs = new List<IJob>();
            using (var jobWorker = ZeebeClient.NewWorker()
                .JobType(jobType)
                .Handler((jobClient, job) =>
                {
                    receivedJobs.Add(job);
                    if (receivedJobs.Count == 3)
                    {
                        signal.Set();
                    }
                })
                .MaxJobsActive(3)
                .Name(jobWorkerName)
                .Timeout(123L)
                .PollInterval(TimeSpan.FromMilliseconds(100))
                .Open())
            {
                jobWorker.IsOpen();
                signal.WaitOne();
            }
        }

        private async Task DeployBpmnFile()
        {
            // 通过UTF-8格式部署file
            var fileContent = File.ReadAllText(this.demoProcessPath);
            await ZeebeClient.NewDeployCommand()
                .AddResourceString(fileContent, Encoding.UTF8, this.demoProcessPath)
                .Send();
        }

        private async Task CreateWorkflowInstance(string bpmnProcessId)
        {
            await ZeebeClient.NewCreateWorkflowInstanceCommand()
                .BpmnProcessId(bpmnProcessId)
                .LatestVersion()
                .Variables("{\"orderId\": 12345}")
                .Send();
        }
    }
}
