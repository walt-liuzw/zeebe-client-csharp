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
using GatewayProtocol;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog;
using NUnit.Framework.Constraints;
using NUnit.Framework.Internal;
using Zeebe.Client.Api.Responses;
using Logger = NLog.Logger;

namespace Zeebe.Client
{
    [TestFixture]
    public class JobWorkerTest : BaseZeebeTest
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [Test]
        public void ShouldSendRequestReceiveResponseAsExpected()
        {
            // given
            var expectedRequest = new ActivateJobsRequest
            {
                Timeout = 123L,
                MaxJobsToActivate = 3,
                Type = "foo",
                Worker = "jobWorker"
            };

            TestService.AddRequestHandler(typeof(ActivateJobsRequest), request => CreateExpectedResponse());

            // when
            var signal = new EventWaitHandle(false, EventResetMode.AutoReset);
            var receivedJobs = new List<IJob>();
            using (var jobWorker = ZeebeClient.NewWorker()
                .JobType("foo")
                .Handler((jobClient, job) =>
                {
                    receivedJobs.Add(job);
                    if (receivedJobs.Count == 3)
                    {
                        signal.Set();
                    }
                })
                .MaxJobsActive(3)
                .Name("jobWorker")
                .Timeout(123L)
                .PollInterval(TimeSpan.FromMilliseconds(100))
                .Open())
            {

                Assert.True(jobWorker.IsOpen());
                signal.WaitOne();
            }

            // then
            var actualRequest = TestService.Requests[0];
            Assert.AreEqual(expectedRequest, actualRequest);

            Assert.AreEqual(receivedJobs.Count, 3);

            AssertJob(receivedJobs[0], 1);
            AssertJob(receivedJobs[1], 2);
            AssertJob(receivedJobs[2], 3);
        }

        [Test]
        public void ShouldSendRequestsWithDifferentAmounts()
        {
            // given
            var expectedRequest = new ActivateJobsRequest
            {
                Timeout = 123L,
                MaxJobsToActivate = 4,
                Type = "foo",
                Worker = "jobWorker"
            };

            var expectedSecondRequest = new ActivateJobsRequest
            {
                Timeout = 123L,
                MaxJobsToActivate = 2, // first response contains 3 jobs and one is handled (blocking) so 2 jobs remain in queue
                            // so we can try to activate 2 new jobs
                Type = "foo",
                Worker = "jobWorker"
            };

            TestService.AddRequestHandler(typeof(ActivateJobsRequest), request => CreateExpectedResponse());

            // when
            var receivedJobs = new List<IJob>();
            using (var jobWorker = ZeebeClient.NewWorker()
                .JobType("foo")
                .Handler((jobClient, job) =>
                {
                    // block job handling
                    using (var signal = new EventWaitHandle(false, EventResetMode.AutoReset))
                    {
                        signal.WaitOne();
                    }
                })
                .MaxJobsActive(4)
                .Name("jobWorker")
                .Timeout(123L)
                .PollInterval(TimeSpan.FromMilliseconds(100))
                .Open())
            {
                Assert.True(jobWorker.IsOpen());
                while (TestService.Requests.Count < 2)
                {
                }
            }

            // then
            var actualRequest = TestService.Requests[0];
            Assert.AreEqual(expectedRequest, actualRequest);

            var actualSecondRequest = TestService.Requests[1];
            Assert.AreEqual(expectedSecondRequest, actualSecondRequest);
        }

        [Test]
        public void ShouldSendRequestWithTimeSpanTimeout()
        {
            // given
            var expectedRequest = new ActivateJobsRequest
            {
                Timeout = 10_000L,
                MaxJobsToActivate = 1,
                Type = "foo",
                Worker = "jobWorker"
            };

            TestService.AddRequestHandler(typeof(ActivateJobsRequest), request => CreateExpectedResponse());

            // when
            var signal = new EventWaitHandle(false, EventResetMode.AutoReset);
            var receivedJobs = new List<IJob>();
            using (var jobWorker = ZeebeClient.NewWorker()
                .JobType("foo")
                .Handler((jobClient, job) =>
                {
                    receivedJobs.Add(job);
                    if (receivedJobs.Count == 3)
                    {
                        signal.Set();
                    }
                })
                .MaxJobsActive(1)
                .Name("jobWorker")
                .Timeout(TimeSpan.FromSeconds(10))
                .PollInterval(TimeSpan.FromMilliseconds(100))
                .Open())
            {

                Assert.True(jobWorker.IsOpen());
                signal.WaitOne();
            }

            // then
            var actualRequest = TestService.Requests[0];
            Assert.AreEqual(expectedRequest, actualRequest);

            Assert.AreEqual(receivedJobs.Count, 3);

            AssertJob(receivedJobs[0], 1);
            AssertJob(receivedJobs[1], 2);
            AssertJob(receivedJobs[2], 3);
        }

        [Test]
        public void ShouldSendRequestWithFetchVariables()
        {
            // given
            var expectedRequest = new ActivateJobsRequest
            {
                Timeout = 10_000L,
                MaxJobsToActivate = 1,
                Type = "foo",
                Worker = "jobWorker",
                FetchVariable = { "foo", "bar", "test" }
            };

            TestService.AddRequestHandler(typeof(ActivateJobsRequest), request => CreateExpectedResponse());

            // when
            var signal = new EventWaitHandle(false, EventResetMode.AutoReset);
            var receivedJobs = new List<IJob>();
            using (var jobWorker = ZeebeClient.NewWorker()
                .JobType("foo")
                .Handler((jobClient, job) =>
                {
                    receivedJobs.Add(job);
                    if (receivedJobs.Count == 3)
                    {
                        signal.Set();
                    }
                })
                .MaxJobsActive(1)
                .Name("jobWorker")
                .Timeout(TimeSpan.FromSeconds(10))
                .FetchVariables("foo", "bar", "test")
                .PollInterval(TimeSpan.FromMilliseconds(100))
                .Open())
            {

                Assert.True(jobWorker.IsOpen());
                signal.WaitOne();
            }

            // then
            var actualRequest = TestService.Requests[0];
            Assert.AreEqual(expectedRequest, actualRequest);

            Assert.AreEqual(receivedJobs.Count, 3);

            AssertJob(receivedJobs[0], 1);
            AssertJob(receivedJobs[1], 2);
            AssertJob(receivedJobs[2], 3);
        }

        [Test]
        public void ShouldSendRequestWithFetchVariablesList()
        {
            // given
            var expectedRequest = new ActivateJobsRequest
            {
                Timeout = 10_000L,
                MaxJobsToActivate = 1,
                Type = "foo",
                Worker = "jobWorker",
                FetchVariable = { "foo", "bar", "test" }
            };
            IList<string> variableNames = new List<string> { "foo", "bar", "test" };
            TestService.AddRequestHandler(typeof(ActivateJobsRequest), request => CreateExpectedResponse());

            // when
            var signal = new EventWaitHandle(false, EventResetMode.AutoReset);
            var receivedJobs = new List<IJob>();
            using (var jobWorker = ZeebeClient.NewWorker()
                .JobType("foo")
                .Handler((jobClient, job) =>
                {
                    receivedJobs.Add(job);
                    if (receivedJobs.Count == 3)
                    {
                        signal.Set();
                    }
                })
                .MaxJobsActive(1)
                .Name("jobWorker")
                .Timeout(TimeSpan.FromSeconds(10))
                .FetchVariables(variableNames)
                .PollInterval(TimeSpan.FromMilliseconds(100))
                .Open())
            {

                Assert.True(jobWorker.IsOpen());
                signal.WaitOne();
            }

            // then
            var actualRequest = TestService.Requests[0];
            Assert.AreEqual(expectedRequest, actualRequest);

            Assert.AreEqual(receivedJobs.Count, 3);

            AssertJob(receivedJobs[0], 1);
            AssertJob(receivedJobs[1], 2);
            AssertJob(receivedJobs[2], 3);
        }

        [Test]
        public void ShouldSendFailCommandOnExceptionInJobHandler()
        {
            // given
            var expectedRequest = new ActivateJobsRequest
            {
                Timeout = 123L,
                MaxJobsToActivate = 1,
                Type = "foo",
                Worker = "jobWorker"
            };

            var expectedFailRequest = new FailJobRequest
            {
                JobKey = 1,
                ErrorMessage = "Job worker 'jobWorker' tried to handle job of type 'foo', but exception occured 'Fail'",
                Retries = 2
            };

            TestService.AddRequestHandler(typeof(ActivateJobsRequest), request => CreateExpectedResponse());

            // when
            using (var jobWorker = ZeebeClient.NewWorker()
                .JobType("foo")
                .Handler((jobClient, job) =>
                {
                    if (job.Key == 1)
                    {
                        throw new Exception("Fail");
                    }
                })
                .MaxJobsActive(1)
                .Name("jobWorker")
                .Timeout(123L)
                .PollInterval(TimeSpan.FromMilliseconds(100))
                .Open())
            {
                Assert.True(jobWorker.IsOpen());
                while (TestService.Requests.Count < 2)
                {
                }
            }

            // then
            var actualRequest = TestService.Requests[0];
            Assert.AreEqual(expectedRequest, actualRequest);

            var actualFailRequest = TestService.Requests[1];
            Assert.AreEqual(expectedFailRequest, actualFailRequest);
        }


        [Test]
        public void ShouldUseAutoCompleteWithWorker()
        {
            // given
            var expectedRequest = new ActivateJobsRequest
            {
                Timeout = 5_000L,
                MaxJobsToActivate = 3,
                Type = "foo",
                Worker = "jobWorker"
            };

            var expectedCompleteRequest = new CompleteJobRequest();
            TestService.AddRequestHandler(typeof(ActivateJobsRequest),
                request => CreateExpectedResponse());

            // when
            using (var jobWorker = ZeebeClient.NewWorker()
                .JobType("foo")
                .Handler((jobClient, job) =>
                {
                    Logger.Info("Handler has seen job '{0}'", job);
                })
                .AutoCompletion()
                .MaxJobsActive(3)
                .Name("jobWorker")
                .Timeout(5_000L)
                .PollInterval(TimeSpan.FromSeconds(5))
                .Open())
            {
                Assert.True(jobWorker.IsOpen());
                while (TestService.Requests.Count < 4)
                {
                }
            }

            // then
            var actualRequest = TestService.Requests[0];
            Assert.AreEqual(expectedRequest, actualRequest);

            var completeJobRequests = TestService.Requests.OfType<CompleteJobRequest>().Select(j => j.JobKey).ToList();
            Assert.AreEqual(3, completeJobRequests.Count);

            Assert.Contains(1, completeJobRequests);
            Assert.Contains(2, completeJobRequests);
            Assert.Contains(3, completeJobRequests);
        }

        public static ActivateJobsResponse CreateExpectedResponse()
        {
            return new ActivateJobsResponse
            {
                Jobs =
                {
                    new ActivatedJob{
                        Key = 1,
                        Worker = "jobWorker",
                        Type = "foo",
                        Variables = "{\"foo\":1}",
                        Retries = 3,
                        Deadline = 123932,
                        JobHeaders = new JobHeaders{
                            BpmnProcessId = "process",
                            ElementId = "job1",
                            ElementInstanceKey = 23,
                            WorkflowDefinitionVersion = 3,
                            WorkflowKey = 21}
                    },
                    new ActivatedJob{
                        Key = 2,
                        Worker = "jobWorker",
                        Type = "foo",
                        Variables = "{\"foo\":2}",
                        Retries = 3,
                        Deadline = 123932,
                        JobHeaders = new JobHeaders{
                            BpmnProcessId = "process",
                            ElementId = "job2",
                            ElementInstanceKey = 23,
                            WorkflowDefinitionVersion = 3,
                            WorkflowKey = 21}
                    },
                    new ActivatedJob{
                        Key = 3,
                        Worker = "jobWorker",
                        Type = "foo",
                        Variables = "{\"foo\":3}",
                        Retries = 3,
                        Deadline = 123932,
                        JobHeaders = new JobHeaders{
                            BpmnProcessId = "process",
                            ElementId = "job3",
                            ElementInstanceKey = 23,
                            WorkflowDefinitionVersion = 3,
                            WorkflowKey = 21}
                    }
                }
            };
        }

        public static void AssertJob(IJob job, int expectedKey)
        {
            Assert.AreEqual(expectedKey, job.Key);
            Assert.AreEqual(3, job.Retries);
            Assert.AreEqual("foo", job.Type);
            Assert.AreEqual("jobWorker", job.Worker);

            Assert.AreEqual("{\"foo\":" + expectedKey + "}", job.Variables);
            var expectedVariables = new Dictionary<string, int> { { "foo", expectedKey } };
            CollectionAssert.AreEquivalent(expectedVariables, job.VariablesAsDictionary);

            Assert.AreEqual("process", job.Headers.BpmnProcessId);
            Assert.AreEqual("job" + expectedKey, job.Headers.ElementId);
            Assert.AreEqual(23, job.Headers.ElementInstanceKey);
            Assert.AreEqual(3, job.Headers.WorkflowDefinitionVersion);
            Assert.AreEqual(21, job.Headers.WorkflowKey);
        }
    }
}