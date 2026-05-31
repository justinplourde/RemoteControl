using Microsoft.VisualStudio.TestTools.UnitTesting;
using Quasar.Common.Messages;
using Quasar.Common.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Quasar.Common.Tests.Messages
{
    [TestClass]
    public class ProtocolSurfaceTests
    {
        private static readonly string[] ExpectedMessageNames =
        {
            "ClientIdentification",
            "ClientIdentificationResult",
            "DoAskElevate",
            "DoChangeRegistryValue",
            "DoClientDisconnect",
            "DoClientReconnect",
            "DoClientUninstall",
            "DoCloseConnection",
            "DoCreateRegistryKey",
            "DoCreateRegistryValue",
            "DoDeleteRegistryKey",
            "DoDeleteRegistryValue",
            "DoKeyboardEvent",
            "DoLoadRegistryKey",
            "DoMouseEvent",
            "DoPathDelete",
            "DoPathRename",
            "DoProcessEnd",
            "DoProcessResponse",
            "DoProcessStart",
            "DoRenameRegistryKey",
            "DoRenameRegistryValue",
            "DoShellExecute",
            "DoShellExecuteResponse",
            "DoShowMessageBox",
            "DoShutdownAction",
            "DoStartupItemAdd",
            "DoStartupItemRemove",
            "DoVisitWebsite",
            "DoWebcamStop",
            "FileTransferCancel",
            "FileTransferChunk",
            "FileTransferComplete",
            "FileTransferRequest",
            "GetChangeRegistryValueResponse",
            "GetConnections",
            "GetConnectionsResponse",
            "GetCreateRegistryKeyResponse",
            "GetCreateRegistryValueResponse",
            "GetDeleteRegistryKeyResponse",
            "GetDeleteRegistryValueResponse",
            "GetDesktop",
            "GetDesktopResponse",
            "GetDirectory",
            "GetDirectoryResponse",
            "GetDrives",
            "GetDrivesResponse",
            "GetKeyloggerLogsDirectory",
            "GetKeyloggerLogsDirectoryResponse",
            "GetMonitors",
            "GetMonitorsResponse",
            "GetPasswords",
            "GetPasswordsResponse",
            "GetProcesses",
            "GetProcessesResponse",
            "GetRegistryKeysResponse",
            "GetRenameRegistryKeyResponse",
            "GetRenameRegistryValueResponse",
            "GetStartupItems",
            "GetStartupItemsResponse",
            "GetSystemInfo",
            "GetSystemInfoResponse",
            "ReverseProxyConnect",
            "ReverseProxyConnectResponse",
            "ReverseProxyData",
            "ReverseProxyDisconnect",
            "SetStatus",
            "SetStatusFileManager",
            "SetUserStatus"
        };

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            TypeRegistry.AddTypesToSerializer(typeof(IMessage), MessageTypes().ToArray());
        }

        [TestMethod, TestCategory("Protocol")]
        public void ModernProtocolSurfaceContainsEveryLegacyMessageContract()
        {
            CollectionAssert.AreEqual(ExpectedMessageNames, MessageTypes().Select(t => t.Name).OrderBy(n => n).ToArray());
        }

        [TestMethod, TestCategory("Protocol")]
        public void EveryModernMessageContractPayloadRoundTripsThroughRegisteredInterface()
        {
            foreach (var messageType in MessageTypes())
            {
                var message = (IMessage)TestObjectFactory.Create(messageType);

                using (var stream = new MemoryStream())
                {
                    using (var writer = new PayloadWriter(stream, true))
                    {
                        writer.WriteMessage(message);
                    }

                    using (var reader = new PayloadReader(stream.ToArray(), (int)stream.Length, false))
                    {
                        Assert.AreEqual(messageType, reader.ReadMessage().GetType(), messageType.FullName);
                    }
                }
            }
        }

        private static IEnumerable<Type> MessageTypes()
        {
            return typeof(IMessage).Assembly.GetTypes()
                .Where(t => typeof(IMessage).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .OrderBy(t => t.Name);
        }

        private static class TestObjectFactory
        {
            public static object Create(Type type)
            {
                return Create(type, new HashSet<Type>());
            }

            private static object Create(Type type, HashSet<Type> stack)
            {
                Type underlyingType = Nullable.GetUnderlyingType(type);
                if (underlyingType != null)
                    return Create(underlyingType, stack);

                if (type == typeof(string))
                    return "sample";
                if (type == typeof(int))
                    return 7;
                if (type == typeof(long))
                    return 12345L;
                if (type == typeof(bool))
                    return true;
                if (type == typeof(byte[]))
                    return new byte[] { 1, 2, 3 };
                if (type == typeof(DateTime))
                    return new DateTime(2026, 05, 31, 12, 30, 00, DateTimeKind.Utc);
                if (type.IsEnum)
                    return Enum.GetValues(type).GetValue(0);
                if (type.IsArray)
                {
                    Type elementType = type.GetElementType();
                    Array array = Array.CreateInstance(elementType, 1);
                    array.SetValue(Create(elementType, stack), 0);
                    return array;
                }
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    Type elementType = type.GetGenericArguments()[0];
                    IList list = (IList)Activator.CreateInstance(type);
                    list.Add(Create(elementType, stack));
                    return list;
                }
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Tuple<,>))
                {
                    Type[] argumentTypes = type.GetGenericArguments();
                    return Activator.CreateInstance(type, Create(argumentTypes[0], stack), Create(argumentTypes[1], stack));
                }
                if (stack.Contains(type))
                    return null;

                stack.Add(type);
                object instance = Activator.CreateInstance(type);
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite))
                {
                    property.SetValue(instance, Create(property.PropertyType, stack));
                }
                stack.Remove(type);
                return instance;
            }
        }
    }
}
