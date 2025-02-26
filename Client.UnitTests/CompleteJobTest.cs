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
using NUnit.Framework;
using System.Threading.Tasks;

namespace Zeebe.Client
{
    [TestFixture]
    public class CompleteJobTest : BaseZeebeTest
    {
        [Test]
        public async Task ShouldSendRequestAsExpected()
        {
            // given
            const string Variables = "{\"foo\":23}";
            const int JobKey = 255;
            var expectedRequest = new CompleteJobRequest
            {
                JobKey = JobKey,
                Variables = Variables
            };

            // when
            await ZeebeClient.NewCompleteJobCommand(JobKey).Variables(Variables).Send();

            // then
            var actualRequest = TestService.Requests[0];

            Assert.AreEqual(expectedRequest, actualRequest);
        }


        [Test]
        public async Task ShouldUseActivatedJobToComplete()
        {
            // given
            const string Variables = "{\"foo\":23}";
            const int JobKey = 255;

            var grpcActivatedJob = new ActivatedJob();
            grpcActivatedJob.Key = JobKey;
            grpcActivatedJob.JobHeaders = new JobHeaders();
            var activatedJob = new Impl.Responses.ActivatedJob(grpcActivatedJob);
            var expectedRequest = new CompleteJobRequest
            {
                JobKey = JobKey,
                Variables = Variables
            };

            // when
            await ZeebeClient.NewCompleteJobCommand(activatedJob).Variables(Variables).Send();

            // then
            var actualRequest = TestService.Requests[0];

            Assert.AreEqual(expectedRequest, actualRequest);
        }
    }
}
