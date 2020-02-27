/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Azure;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Data;
using TeamCloud.Orchestrator.Orchestrations.Projects.Activities;
using TeamCloud.Orchestrator.Orchestrations.Providers.Activities;

namespace TeamCloud.Orchestrator.Orchestrations.Providers
{
    [EternalOrchestration(EternalInstanceId)]
    public class ProviderRegisterOrchestration
    {
        public const string EternalInstanceId = "c0ed8d5a-ca7a-4186-84bd-062a8bac0d3a";
        private readonly IAzureSessionService azureSessionService;

        public ProviderRegisterOrchestration(IAzureSessionService azureSessionService)
        {
            this.azureSessionService = azureSessionService ?? throw new ArgumentNullException(nameof(azureSessionService));
        }

        [FunctionName(nameof(ProviderRegisterOrchestration))]
        public async Task RunOrchestration(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var isEternalInstance = functionContext.InstanceId.Equals(EternalInstanceId, StringComparison.OrdinalIgnoreCase);
            var nextRegistration = functionContext.CurrentUtcDateTime.NextHour(); // default set to the next full hour

            try
            {
                var provider = functionContext.GetInput<Provider>();

                if (provider is null)
                {
                    functionContext.SetCustomStatus("Registering Providers ...", log);
                }
                else
                {
                    functionContext.SetCustomStatus($"Registering Provider {provider.Id} ...", log);
                }

                var teamCloud = await functionContext
                    .CallActivityAsync<TeamCloudInstance>(nameof(TeamCloudGetActivity), null)
                    .ConfigureAwait(true);

                var systemUser = await functionContext
                    .CallActivityAsync<User>(nameof(SystemUserActivity), null)
                    .ConfigureAwait(true);

                var providerCommandTasks = (isEternalInstance || provider is null)
                    ? GetProviderRegisterCommandTasks(functionContext, teamCloud.Providers, systemUser)
                    : GetProviderRegisterCommandTasks(functionContext, Enumerable.Repeat(provider, 1), systemUser);

                var providerCommandResults = await Task
                    .WhenAll(providerCommandTasks)
                    .ConfigureAwait(true);

                using (await functionContext.LockAsync(teamCloud.GetEntityId()).ConfigureAwait(true))
                {
                    // we need to re-get the current TeamCloud instance
                    // to ensure we update the latest version inside
                    // our critical section

                    teamCloud = await functionContext
                        .CallActivityAsync<TeamCloudInstance>(nameof(TeamCloudGetActivity), null)
                        .ConfigureAwait(true);

                    var providerCommandApplied = false;
                    var providerCommandExceptions = new List<Exception>();

                    foreach (var providerCommandResult in providerCommandResults)
                    {
                        if (providerCommandResult.Errors.Any())
                        {
                            providerCommandExceptions.AddRange(providerCommandResult.Errors);
                        }
                        else
                        {
                            provider = teamCloud.Providers.FirstOrDefault(p => p.Id == providerCommandResult.ProviderId);

                            if (provider != null)
                            {
                                provider.PrincipalId = providerCommandResult.Result.PrincipalId;
                                provider.Properties = provider.Properties.Merge(providerCommandResult.Result.Properties);

                                providerCommandApplied = true;
                            }
                        }
                    }

                    if (providerCommandApplied)
                    {
                        await functionContext
                            .CallActivityAsync(nameof(TeamCloudSetActivity), teamCloud)
                            .ConfigureAwait(true);
                    }

                    if (providerCommandExceptions.Any())
                        throw new AggregateException(providerCommandExceptions);
                }

                functionContext.SetCustomStatus($"Provider registration succeeded", log);
            }
            catch (Exception exc)
            {
                // in case of an exception we will switch to a shorter
                // registration cycle to handle hiccups on the provider side

                nextRegistration = functionContext.CurrentUtcDateTime.AddMinutes(5);

                functionContext.SetCustomStatus($"Provider registration failed", log, exc);

                if (!isEternalInstance)
                    throw; // for non-eternal instances we bubble the catched exception
            }
            finally
            {
                // there is no way to define an orchestration as "eternal only"
                // to ensure that only one eternal version exists we need to
                // compare the current instance id with the eternal instance id
                // and only reschedule if they are equal

                if (isEternalInstance)
                {
                    // there is a chance that our next registration cycle
                    // is schedule in the past. we need to ensure that this
                    // doesn't happen to avoid issues on the eternal orchestration.

                    if (nextRegistration < functionContext.CurrentUtcDateTime)
                        nextRegistration = nextRegistration.NextHour();

                    log.LogInformation($"Provider registration scheduled for {nextRegistration}");

                    await functionContext
                        .CreateTimer(nextRegistration, CancellationToken.None)
                        .ConfigureAwait(true);

                    functionContext.ContinueAsNew(null);
                }
            }
        }

        private IEnumerable<Task<ProviderRegisterCommandResult>> GetProviderRegisterCommandTasks(IDurableOrchestrationContext context, IEnumerable<Provider> providers, User user)
            => providers.Select(provider =>
            {
                var command = new ProviderRegisterCommand(Guid.NewGuid(), provider.Id, user, new ProviderConfiguration
                {
                    Properties = provider.Properties
                });

                return context.CallSubOrchestratorAsync<ProviderRegisterCommandResult>(nameof(ProviderCommandOrchestration), (provider, command));
            });
    }
}
