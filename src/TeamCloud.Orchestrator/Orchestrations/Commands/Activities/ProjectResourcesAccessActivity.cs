﻿/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Azure;
using TeamCloud.Azure.Resources;
using TeamCloud.Azure.Resources.Typed;
using TeamCloud.Data;
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;

namespace TeamCloud.Orchestrator.Orchestrations.Commands.Activities
{
    public class ProjectResourcesAccessActivity
    {
        private readonly IAzureSessionService azureSessionService;
        private readonly IAzureResourceService azureResourceService;
        private readonly IProjectsRepositoryReadOnly projectsRepository;

        public ProjectResourcesAccessActivity(IAzureSessionService azureSessionService, IAzureResourceService azureResourceService, IProjectsRepositoryReadOnly projectsRepository)
        {
            this.azureSessionService = azureSessionService ?? throw new ArgumentNullException(nameof(azureSessionService));
            this.azureResourceService = azureResourceService ?? throw new ArgumentNullException(nameof(azureResourceService));
            this.projectsRepository = projectsRepository ?? throw new ArgumentNullException(nameof(projectsRepository));
        }

        [FunctionName(nameof(ProjectResourcesAccessActivity))]
        [RetryOptions(3)]
        public async Task RunActivity(
            [ActivityTrigger] IDurableActivityContext functionContext)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var (projectId, principalId) = functionContext.GetInput<(Guid, Guid)>();

            var project = await projectsRepository
                .GetAsync(projectId)
                .ConfigureAwait(false);

            if (project != null)
            {
                var tasks = new List<Task>();

                if (!string.IsNullOrEmpty(project.ResourceGroup?.ResourceGroupId))
                    tasks.Add(EnsureResourceGroupAccessAsync(project, principalId));

                if (!string.IsNullOrEmpty(project.KeyVault?.VaultId))
                    tasks.Add(EnsureKeyVaultAccessAsync(project, principalId));

                await Task
                    .WhenAll(tasks)
                    .ConfigureAwait(false);
            }
        }

        private async Task EnsureResourceGroupAccessAsync(Project project, Guid principalId)
        {
            var resourceGroup = await azureResourceService
                 .GetResourceGroupAsync(project.ResourceGroup.SubscriptionId, project.ResourceGroup.ResourceGroupName, throwIfNotExists: true)
                 .ConfigureAwait(false);

            await resourceGroup
                .AddRoleAssignmentAsync(principalId, AzureRoleDefinition.Contributor)
                .ConfigureAwait(false);

            await resourceGroup
                .AddRoleAssignmentAsync(principalId, AzureRoleDefinition.UserAccessAdministrator)
                .ConfigureAwait(false);
        }

        private async Task EnsureKeyVaultAccessAsync(Project project, Guid principalId)
        {
            var keyVault = await azureResourceService
                .GetResourceAsync<AzureKeyVaultResource>(project.KeyVault.VaultId, throwIfNotExists: true)
                .ConfigureAwait(false);

            var systemIdentity = await azureSessionService
                .GetIdentityAsync()
                .ConfigureAwait(false);

            if (systemIdentity.ObjectId == principalId)
            {
                await keyVault
                    .SetAllCertificatePermissionsAsync(principalId)
                    .ConfigureAwait(false);

                await keyVault
                    .SetAllKeyPermissionsAsync(principalId)
                    .ConfigureAwait(false);

                await keyVault
                    .SetAllSecretPermissionsAsync(principalId)
                    .ConfigureAwait(false);
            }
            else
            {
                await keyVault
                    .SetCertificatePermissionsAsync(principalId, CertificatePermissions.Get, CertificatePermissions.List)
                    .ConfigureAwait(false);

                await keyVault
                    .SetKeyPermissionsAsync(principalId, KeyPermissions.Get, KeyPermissions.List)
                    .ConfigureAwait(false);

                await keyVault
                    .SetSecretPermissionsAsync(principalId, SecretPermissions.Get, SecretPermissions.List)
                    .ConfigureAwait(false);
            }
        }

    }
}
