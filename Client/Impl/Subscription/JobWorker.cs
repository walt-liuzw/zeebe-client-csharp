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
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Zeebe.Client.Api.Clients;
using Zeebe.Client.Api.Commands;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Subscription;
using Zeebe.Client.Impl.Commands;

namespace Zeebe.Client.Impl.Subscription
{
    public class JobWorker : IJobWorker
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private const string JobFailMessage = "Job worker '{0}' tried to handle job of type '{1}', but exception occured '{2}'";

        private readonly int maxJobsActive;
        private readonly ConcurrentQueue<IJob> workItems = new ConcurrentQueue<IJob>();

        private readonly ActivateJobsRequest activeRequest;
        private readonly JobActivator activator;
        private readonly JobHandler jobHandler;
        private readonly JobClientWrapper jobClient;
        private readonly bool autoCompletion;
        private readonly TimeSpan pollInterval;
        private readonly CancellationTokenSource source;

        private readonly EventWaitHandle handleSignal = new EventWaitHandle(false, EventResetMode.AutoReset);
        private readonly EventWaitHandle pollSignal = new EventWaitHandle(false, EventResetMode.AutoReset);

        private volatile bool isRunning;


        internal JobWorker(JobWorkerBuilder builder)
        {
            source = new CancellationTokenSource();
            activator = new JobActivator(builder.Client);
            activeRequest = builder.Request;
            maxJobsActive = activeRequest.MaxJobsToActivate;
            pollInterval = builder.PollInterval();
            jobClient = new JobClientWrapper(builder.JobClient);
            jobHandler = builder.Handler();
            autoCompletion = builder.AutoCompletionEnabled();
        }


        internal void Open()
        {
            isRunning = true;
            var cancellationToken = source.Token;

            var taskFactory = new TaskFactory();

            taskFactory.StartNew(async () =>
                await Poll(cancellationToken)
                    .ContinueWith(t => Logger.Error(t.Exception),
                        TaskContinuationOptions.OnlyOnFaulted), cancellationToken
            ).ContinueWith(
                    t => Logger.Error(t.Exception),
                    TaskContinuationOptions.OnlyOnFaulted);

            taskFactory.StartNew(() => HandleActivatedJobs(cancellationToken), cancellationToken)
                .ContinueWith(t => Logger.Error(t.Exception),
                    TaskContinuationOptions.OnlyOnFaulted);

            Logger.Debug("Job worker ({0}) for job type '{1}' has been opened.", activeRequest.Worker, activeRequest.Type);
        }

        private void HandleActivatedJobs(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!workItems.IsEmpty)
                {
                    bool success = workItems.TryDequeue(out IJob activatedJob);

                    if (success)
                    {
                        try
                        {
                            jobHandler(jobClient, activatedJob);
                            if (!jobClient.ClientWasUsed && autoCompletion)
                            {
                                Logger.Debug("Job worker ({0}) will auto complete job with key '{1}'", activeRequest.Worker, activatedJob.Key);
                                jobClient.NewCompleteJobCommand(activatedJob).Send();
                            }
                        }
                        catch (Exception exception)
                        {
                            FailActivatedJob(activatedJob, exception);
                        }
                        finally
                        {
                            jobClient.Reset();
                        }
                    }
                    else
                    {
                        pollSignal.Set();
                    }
                }
                else
                {
                    handleSignal.WaitOne();
                }
            }
        }

        private void FailActivatedJob(IJob activatedJob, Exception exception)
        {
            var errorMessage = string.Format(JobFailMessage,
                activatedJob.Worker,
                activatedJob.Type,
                exception.Message);

            jobClient.NewFailCommand(activatedJob.Key)
                .Retries(activatedJob.Retries - 1)
                .ErrorMessage(errorMessage)
                .Send();
            Logger.Error(exception, errorMessage);
        }

        private async Task Poll(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (workItems.Count < maxJobsActive)
                {
                    await PollJobs(cancellationToken);
                }
                pollSignal.WaitOne(pollInterval);
            }
        }

        private async Task PollJobs(CancellationToken cancellationToken)
        {
            var jobCount = maxJobsActive - workItems.Count;
            activeRequest.MaxJobsToActivate = jobCount;

            var response = await activator.SendActivateRequest(activeRequest, cancellationToken);

            Logger.Debug("Job worker ({0}) activated '{1}' of '{2}' successfully.", activeRequest.Worker, response.Jobs.Count, jobCount);
            foreach (var job in response.Jobs)
            {
                workItems.Enqueue(job);
            }

            handleSignal.Set();
        }

        public void Dispose()
        {
            source.Cancel();
            isRunning = false;
        }

        public bool IsOpen()
        {
            return isRunning;
        }

        public bool IsClosed()
        {
            return !isRunning;
        }

        private class JobClientWrapper : IJobClient
        {
            private IJobClient Client { get; }

            public bool ClientWasUsed { get; private set; }


            public JobClientWrapper(IJobClient client)
            {
                Client = client;
                ClientWasUsed = false;
            }

            public ICompleteJobCommandStep1 NewCompleteJobCommand(long jobKey)
            {
                ClientWasUsed = true;
                return Client.NewCompleteJobCommand(jobKey);
            }

            public IFailJobCommandStep1 NewFailCommand(long jobKey)
            {
                ClientWasUsed = true;
                return Client.NewFailCommand(jobKey);
            }

            public void Reset()
            {
                ClientWasUsed = false;
            }

            public ICompleteJobCommandStep1 NewCompleteJobCommand(IJob activatedJob)
            {
                ClientWasUsed = true;
                return Client.NewCompleteJobCommand(activatedJob);
            }
        }
    }
}