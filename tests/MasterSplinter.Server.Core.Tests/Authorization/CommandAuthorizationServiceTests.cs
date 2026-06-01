using MasterSplinter.Server.Core.Authorization;
using MasterSplinter.Server.Core.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Quasar.Common.Enums;
using Quasar.Common.Messages;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.Tests.Authorization
{
    [TestClass]
    public class CommandAuthorizationServiceTests
    {
        [TestMethod, TestCategory("ServerCore")]
        public async Task ReadOnlyCommandsDoNotNeedPermissionOrConsentGrants()
        {
            var permissionService = new RecordingPermissionService(false);
            var consentService = new RecordingConsentService(false);
            var service = new CommandAuthorizationService(permissionService, consentService);
            var request = CommandDispatchRequest.Create("client-1", new GetSystemInfo());
            CommandSafetyMetadata safetyMetadata = CommandSafetyMetadata.ReadOnly(CommandSafetyClass.ReadOnlyInventory);

            CommandDispatchAuthorization authorization = await service.AuthorizeAsync(
                null,
                request,
                safetyMetadata,
                CancellationToken.None);

            Assert.IsTrue(authorization.OperatorHasPermission);
            Assert.IsTrue(authorization.ClientConsentGranted);
            Assert.AreEqual(0, permissionService.Requests.Count);
            Assert.AreEqual(0, consentService.Requests.Count);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task PermissionGatedCommandsUseOperatorPermissionService()
        {
            var permissionService = new RecordingPermissionService(true);
            var consentService = new RecordingConsentService(false);
            var service = new CommandAuthorizationService(permissionService, consentService);
            var operatorIdentity = new OperatorIdentity("operator-1", "Operator One");
            var request = CommandDispatchRequest.Create(
                "client-1",
                new DoPathDelete { Path = "C:\\Temp\\old.txt", PathType = FileType.File });
            CommandSafetyMetadata safetyMetadata =
                CommandSafetyMetadata.Controlled(CommandSafetyClass.FileWrite, requiresConsent: false);

            CommandDispatchAuthorization authorization = await service.AuthorizeAsync(
                operatorIdentity,
                request,
                safetyMetadata,
                CancellationToken.None);

            Assert.IsTrue(authorization.OperatorHasPermission);
            Assert.IsTrue(authorization.ClientConsentGranted);
            Assert.AreEqual(OperatorPermission.FileWrite, permissionService.Requests[0].Permission);
            Assert.AreSame(operatorIdentity, permissionService.Requests[0].OperatorIdentity);
            Assert.AreEqual(0, consentService.Requests.Count);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task MissingOperatorCannotSatisfyPermissionRequirement()
        {
            var service = new CommandAuthorizationService(
                new RecordingPermissionService(true),
                new RecordingConsentService(true));
            var request = CommandDispatchRequest.Create(
                "client-1",
                new DoPathDelete { Path = "C:\\Temp\\old.txt", PathType = FileType.File });
            CommandSafetyMetadata safetyMetadata =
                CommandSafetyMetadata.Controlled(CommandSafetyClass.FileWrite, requiresConsent: false);

            CommandDispatchAuthorization authorization = await service.AuthorizeAsync(
                null,
                request,
                safetyMetadata,
                CancellationToken.None);

            Assert.IsFalse(authorization.OperatorHasPermission);
            Assert.IsTrue(authorization.ClientConsentGranted);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task ConsentGatedCommandsUseConsentService()
        {
            var permissionService = new RecordingPermissionService(true);
            var consentService = new RecordingConsentService(true);
            var service = new CommandAuthorizationService(permissionService, consentService);
            var operatorIdentity = new OperatorIdentity("operator-1", "Operator One");
            var request = CommandDispatchRequest.Create("client-1", new DoShellExecute { Command = "whoami" });
            CommandSafetyMetadata safetyMetadata =
                CommandSafetyMetadata.Controlled(CommandSafetyClass.Execution, requiresConsent: true);

            CommandDispatchAuthorization authorization = await service.AuthorizeAsync(
                operatorIdentity,
                request,
                safetyMetadata,
                CancellationToken.None);

            Assert.IsTrue(authorization.OperatorHasPermission);
            Assert.IsTrue(authorization.ClientConsentGranted);
            Assert.AreEqual(OperatorPermission.Execution, permissionService.Requests[0].Permission);
            Assert.AreEqual("client-1", consentService.Requests[0].ClientId);
            Assert.AreSame(operatorIdentity, consentService.Requests[0].OperatorIdentity);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task ConsentGatedCommandsReflectDeniedConsent()
        {
            var service = new CommandAuthorizationService(
                new RecordingPermissionService(true),
                new RecordingConsentService(false));
            var operatorIdentity = new OperatorIdentity("operator-1", "Operator One");
            var request = CommandDispatchRequest.Create("client-1", new DoShellExecute { Command = "whoami" });
            CommandSafetyMetadata safetyMetadata =
                CommandSafetyMetadata.Controlled(CommandSafetyClass.Execution, requiresConsent: true);

            CommandDispatchAuthorization authorization = await service.AuthorizeAsync(
                operatorIdentity,
                request,
                safetyMetadata,
                CancellationToken.None);

            Assert.IsTrue(authorization.OperatorHasPermission);
            Assert.IsFalse(authorization.ClientConsentGranted);
        }

        [TestMethod, TestCategory("ServerCore")]
        public void DispatchRequestCanBeCopiedWithAuthorization()
        {
            var request = new CommandDispatchRequest(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                "client-1",
                new DoShellExecute { Command = "whoami" },
                "operator-1",
                "unit-test");
            var authorization = new CommandDispatchAuthorization(true, true);

            CommandDispatchRequest authorized = request.WithAuthorization(authorization);

            Assert.AreEqual(request.CorrelationId, authorized.CorrelationId);
            Assert.AreEqual(request.ClientId, authorized.ClientId);
            Assert.AreSame(request.Message, authorized.Message);
            Assert.AreEqual(request.OperatorId, authorized.OperatorId);
            Assert.AreEqual(request.Source, authorized.Source);
            Assert.AreSame(authorization, authorized.Authorization);
        }

        private sealed class RecordingPermissionService : IOperatorPermissionService
        {
            private readonly bool _result;

            public RecordingPermissionService(bool result)
            {
                _result = result;
            }

            public List<PermissionRequest> Requests { get; } = new List<PermissionRequest>();

            public Task<bool> HasPermissionAsync(
                OperatorIdentity operatorIdentity,
                OperatorPermission permission,
                CommandDispatchRequest request,
                CommandSafetyMetadata safetyMetadata,
                CancellationToken cancellationToken)
            {
                Requests.Add(new PermissionRequest(operatorIdentity, permission, request, safetyMetadata));
                return Task.FromResult(_result);
            }
        }

        private sealed class RecordingConsentService : IClientConsentService
        {
            private readonly bool _result;

            public RecordingConsentService(bool result)
            {
                _result = result;
            }

            public List<ConsentRequest> Requests { get; } = new List<ConsentRequest>();

            public Task<bool> HasConsentAsync(
                string clientId,
                OperatorIdentity operatorIdentity,
                CommandDispatchRequest request,
                CommandSafetyMetadata safetyMetadata,
                CancellationToken cancellationToken)
            {
                Requests.Add(new ConsentRequest(clientId, operatorIdentity, request, safetyMetadata));
                return Task.FromResult(_result);
            }
        }

        private sealed class PermissionRequest
        {
            public PermissionRequest(
                OperatorIdentity operatorIdentity,
                OperatorPermission permission,
                CommandDispatchRequest request,
                CommandSafetyMetadata safetyMetadata)
            {
                OperatorIdentity = operatorIdentity;
                Permission = permission;
                Request = request;
                SafetyMetadata = safetyMetadata;
            }

            public OperatorIdentity OperatorIdentity { get; }

            public OperatorPermission Permission { get; }

            public CommandDispatchRequest Request { get; }

            public CommandSafetyMetadata SafetyMetadata { get; }
        }

        private sealed class ConsentRequest
        {
            public ConsentRequest(
                string clientId,
                OperatorIdentity operatorIdentity,
                CommandDispatchRequest request,
                CommandSafetyMetadata safetyMetadata)
            {
                ClientId = clientId;
                OperatorIdentity = operatorIdentity;
                Request = request;
                SafetyMetadata = safetyMetadata;
            }

            public string ClientId { get; }

            public OperatorIdentity OperatorIdentity { get; }

            public CommandDispatchRequest Request { get; }

            public CommandSafetyMetadata SafetyMetadata { get; }
        }
    }
}
