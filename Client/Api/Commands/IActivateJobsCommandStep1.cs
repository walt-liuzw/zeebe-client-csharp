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
using System;
using System.Collections.Generic;
using Zeebe.Client.Api.Responses;

namespace Zeebe.Client.Api.Commands
{
    public interface IActivateJobsCommandStep1
    {
        /// <summary>
        /// Set the type of jobs to work on.
        /// </summary>
        /// <param name="jobType">the type of jobs (e.g. "payment")</param>
        /// <returns>the builder for this command</returns>
        IActivateJobsCommandStep2 JobType(string jobType);
    }

    public interface IActivateJobsCommandStep2
    {
        /// <summary>
        /// Set the maximal amount of jobs to activate. If less jobs are avaiable for activation the
        /// command will return a list with fewer jobs.
        /// </summary>
        /// <param name="amount">the maximal number of jobs to activate</param>
        /// <returns>the builder for this command</returns>
        IActivateJobsCommandStep3 Limit(int amount);
    }

    public interface IActivateJobsCommandStep3 : IFinalCommandStep<IActivateJobsResponse>
    {
        /// <summary>
        /// Set the time for how long a job is exclusively assigned for this subscription.
        ///
        /// <p>In this time, the job can not be assigned by other subscriptions to ensure that only one
        /// subscription work on the job. When the time is over then the job can be assigned again by
        /// this or other subscription if it's not completed yet.
        ///
        /// <p>If no timeout is set, then the default is used from the configuration.
        /// </summary>
        /// 
        /// <param name="timeout">the time in milliseconds</param>
        /// <returns>the builder for this command. Call {@link #send()} to complete the command and send
        ///     it to the broker.</returns>
        IActivateJobsCommandStep3 Timeout(long timeout);

        /// <summary>
        /// Set the time for how long a job is exclusively assigned for this subscription.
        ///
        /// <p>In this time, the job can not be assigned by other subscriptions to ensure that only one
        /// subscription work on the job. When the time is over then the job can be assigned again by
        /// this or other subscription if it's not completed yet.
        ///
        /// <p>If no time is set then the default is used from the configuration.
        /// </summary>
        /// <param name="timeout">the time as time span (e.g. "TimeSpan.FromMinutes(10)")</param>
        /// <returns>the builder for this command. Call {@link #send()} to complete the command and send
        ///     it to the broker.</returns>
        IActivateJobsCommandStep3 Timeout(TimeSpan timeout);

        /// <summary>
        /// Set the name of the job worker.
        ///
        /// <p>This name is used to identify the worker which activated the jobs. Its main purpose is for
        /// monitoring and auditing. Commands on activated jobs do not check the worker name, i.e.
        /// complete or fail job.
        ///
        /// <p>If no name is set then the default is used from the configuration.
        /// </summary>
        /// <param name="workerName">the name of the worker (e.g. "payment-service")</param>
        /// <returns>the builder for this command. Call {@link #send()} to complete the command and send
        ///     it to the broker.</returns>
        IActivateJobsCommandStep3 WorkerName(string workerName);

        /// <summary>
        /// Set a list of variable names which should be fetch on job activation.
        ///
        /// <p>The jobs which are activated by this command will only contain variables from this list in
        /// their payload.
        ///
        /// <p>This can be used to limit the number of variables in the payload of the activated jobs.
        /// </summary>
        /// <param name="fetchVariables">list of variables names to fetch on activation</param>
        /// <returns>the builder for this command. Call {@link #send()} to complete the command and send
        ///     it to the broker.</returns>
        IActivateJobsCommandStep3 FetchVariables(IList<string> fetchVariables);

        /// <summary>
        /// Set a list of variable names which should be fetch on job activation.
        ///
        /// <p>The jobs which are activated by this command will only contain variables from this list in
        /// their payload.
        ///
        /// <p>This can be used to limit the number of variables in the payload of the activated jobs.
        /// </summary>
        /// <param name="fetchVariables">list of variables names to fetch on activation</param>
        /// <returns>
        /// the builder for this command. Call {@link #send()} to complete the command and send 
        /// it to the broker.
        /// </returns>
        IActivateJobsCommandStep3 FetchVariables(params string[] fetchVariables);
    }
}