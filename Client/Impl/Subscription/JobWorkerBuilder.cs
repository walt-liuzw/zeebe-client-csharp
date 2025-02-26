﻿//
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
using GatewayProtocol;
using System;
using System.Collections.Generic;
using Zeebe.Client.Api.Clients;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Subscription;

namespace Zeebe.Client.Impl.Subscription
{
    public class JobWorkerBuilder : IJobWorkerBuilderStep1, IJobWorkerBuilderStep2, IJobWorkerBuilderStep3
    {
        private TimeSpan pollInterval;
        private JobHandler handler;
        private bool autoCompletion;

        internal Gateway.GatewayClient Client { get; }
        internal ActivateJobsRequest Request { get; } = new ActivateJobsRequest();
        internal IJobClient JobClient { get; }

        public JobWorkerBuilder(Gateway.GatewayClient client, IJobClient jobClient)
        {
            Client = client;
            JobClient = jobClient;
        }

        public IJobWorkerBuilderStep2 JobType(string type)
        {
            Request.Type = type;
            return this;
        }

        public IJobWorkerBuilderStep3 Handler(JobHandler handler)
        {
            this.handler = handler;
            return this;
        }

        internal JobHandler Handler()
        {
            return handler;
        }

        public IJobWorkerBuilderStep3 Timeout(long timeout)
        {
            Request.Timeout = timeout;
            return this;
        }

        public IJobWorkerBuilderStep3 Timeout(TimeSpan timeout)
        {
            Request.Timeout = (long)timeout.TotalMilliseconds;
            return this;
        }

        public IJobWorkerBuilderStep3 Name(string workerName)
        {
            Request.Worker = workerName;
            return this;
        }

        public IJobWorkerBuilderStep3 MaxJobsActive(int maxJobsActive)
        {
            Request.MaxJobsToActivate = maxJobsActive;
            return this;
        }

        public IJobWorkerBuilderStep3 FetchVariables(IList<string> fetchVariables)
        {
            Request.FetchVariable.AddRange(fetchVariables);
            return this;
        }

        public IJobWorkerBuilderStep3 FetchVariables(params string[] fetchVariables)
        {
            Request.FetchVariable.AddRange(fetchVariables);
            return this;
        }

        public IJobWorkerBuilderStep3 PollInterval(TimeSpan pollInterval)
        {
            this.pollInterval = pollInterval;
            return this;
        }

        internal TimeSpan PollInterval()
        {
            return pollInterval;
        }


        public IJobWorkerBuilderStep3 AutoCompletion()
        {
            autoCompletion = true;
            return this;
        }

        internal bool AutoCompletionEnabled()
        {
            return autoCompletion;
        }

        public IJobWorker Open()
        {
            var worker = new JobWorker(this);

            worker.Open();

            return worker;
        }

    }
}
