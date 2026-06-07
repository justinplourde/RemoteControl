using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf.Meta;
using MasterSplinter.Common.Messages;
using MasterSplinter.Common.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MasterSplinter.Common.Tests.Messages
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

        private static readonly IReadOnlyDictionary<string, string> ExpectedWireFixtures = new Dictionary<string, string>
        {
            ["ClientIdentification"] = "0A0673616D706C65120673616D706C651A0673616D706C65220673616D706C652A0673616D706C6530073A0673616D706C65420673616D706C654A0673616D706C65520673616D706C655A0673616D706C656203010203A2060408071007AA06080A0673616D706C65",
            ["ClientIdentificationResult"] = "0801",
            ["DoAskElevate"] = "",
            ["DoChangeRegistryValue"] = "0A0673616D706C65120D0A0673616D706C651A03010203",
            ["DoClientDisconnect"] = "",
            ["DoClientReconnect"] = "",
            ["DoClientUninstall"] = "",
            ["DoCloseConnection"] = "0A0673616D706C651A0673616D706C65",
            ["DoCreateRegistryKey"] = "0A0673616D706C65",
            ["DoCreateRegistryValue"] = "0A0673616D706C65",
            ["DoDeleteRegistryKey"] = "0A0673616D706C65120673616D706C65",
            ["DoDeleteRegistryValue"] = "0A0673616D706C65120673616D706C65",
            ["DoKeyboardEvent"] = "1001",
            ["DoLoadRegistryKey"] = "0A0673616D706C65",
            ["DoMouseEvent"] = "1001180720072807",
            ["DoPathDelete"] = "0A0673616D706C65",
            ["DoPathRename"] = "0A0673616D706C65120673616D706C65",
            ["DoProcessEnd"] = "0807",
            ["DoProcessResponse"] = "1001",
            ["DoProcessStart"] = "0A0673616D706C65120673616D706C651801",
            ["DoRenameRegistryKey"] = "0A0673616D706C65120673616D706C651A0673616D706C65",
            ["DoRenameRegistryValue"] = "0A0673616D706C65120673616D706C651A0673616D706C65",
            ["DoShellExecute"] = "0A0673616D706C65",
            ["DoShellExecuteResponse"] = "0A0673616D706C651001",
            ["DoShowMessageBox"] = "0A0673616D706C65120673616D706C651A0673616D706C65220673616D706C65",
            ["DoShutdownAction"] = "",
            ["DoStartupItemAdd"] = "0A100A0673616D706C65120673616D706C65",
            ["DoStartupItemRemove"] = "0A100A0673616D706C65120673616D706C65",
            ["DoVisitWebsite"] = "0A0673616D706C651001",
            ["DoWebcamStop"] = "",
            ["FileTransferCancel"] = "0807120673616D706C65",
            ["FileTransferChunk"] = "0807120673616D706C6518B960220808B9601203010203",
            ["FileTransferComplete"] = "0807120673616D706C65",
            ["FileTransferRequest"] = "0807120673616D706C65",
            ["GetChangeRegistryValueResponse"] = "0A0673616D706C65120D0A0673616D706C651A030102031801220673616D706C65",
            ["GetConnections"] = "",
            ["GetConnectionsResponse"] = "0A1A0A0673616D706C65120673616D706C65220673616D706C653001",
            ["GetCreateRegistryKeyResponse"] = "0A0673616D706C6512190A0673616D706C65120D0A0673616D706C651A0301020318011801220673616D706C65",
            ["GetCreateRegistryValueResponse"] = "0A0673616D706C65120D0A0673616D706C651A030102031801220673616D706C65",
            ["GetDeleteRegistryKeyResponse"] = "0A0673616D706C65120673616D706C651801220673616D706C65",
            ["GetDeleteRegistryValueResponse"] = "0A0673616D706C65120673616D706C651801220673616D706C65",
            ["GetDesktop"] = "080110071807",
            ["GetDesktopResponse"] = "0A0301020310071807220408071007",
            ["GetDirectory"] = "0A0673616D706C65",
            ["GetDirectoryResponse"] = "0A0673616D706C651216120673616D706C6518B960220708DCF1A51C10022802",
            ["GetDrives"] = "",
            ["GetDrivesResponse"] = "0A100A0673616D706C65120673616D706C65",
            ["GetKeyloggerLogsDirectory"] = "",
            ["GetKeyloggerLogsDirectoryResponse"] = "0A0673616D706C65",
            ["GetMonitors"] = "",
            ["GetMonitorsResponse"] = "0807",
            ["GetPasswords"] = "",
            ["GetPasswordsResponse"] = "0A200A0673616D706C65120673616D706C651A0673616D706C65220673616D706C65",
            ["GetProcesses"] = "",
            ["GetProcessesResponse"] = "0A120A0673616D706C6510071A0673616D706C65",
            ["GetRegistryKeysResponse"] = "0A190A0673616D706C65120D0A0673616D706C651A030102031801120673616D706C651801220673616D706C65",
            ["GetRenameRegistryKeyResponse"] = "0A0673616D706C65120673616D706C651A0673616D706C6520012A0673616D706C65",
            ["GetRenameRegistryValueResponse"] = "0A0673616D706C65120673616D706C651A0673616D706C6520012A0673616D706C65",
            ["GetStartupItems"] = "",
            ["GetStartupItemsResponse"] = "0A100A0673616D706C65120673616D706C65",
            ["GetSystemInfo"] = "",
            ["GetSystemInfoResponse"] = "0A100A0673616D706C65120673616D706C65",
            ["ReverseProxyConnect"] = "0807120673616D706C651807",
            ["ReverseProxyConnectResponse"] = "080710011A0301020320072A0673616D706C65",
            ["ReverseProxyData"] = "08071203010203",
            ["ReverseProxyDisconnect"] = "0807",
            ["SetStatus"] = "0A0673616D706C65",
            ["SetStatusFileManager"] = "0A0673616D706C651001",
            ["SetUserStatus"] = ""
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

        [TestMethod, TestCategory("Protocol")]
        public void EveryModernMessageContractEmitsPinnedRepresentativeFixture()
        {
            CollectionAssert.AreEqual(ExpectedMessageNames, ExpectedWireFixtures.Keys.OrderBy(k => k).ToArray());

            foreach (var messageType in MessageTypes())
            {
                object message = TestObjectFactory.Create(messageType);
                byte[] actual = SerializeWithFreshModel(message);
                byte[] expected = Convert.FromHexString(ExpectedWireFixtures[messageType.Name]);

                CollectionAssert.AreEqual(expected, actual, messageType.FullName);
            }
        }

        private static IEnumerable<Type> MessageTypes()
        {
            return typeof(IMessage).Assembly.GetTypes()
                .Where(t => typeof(IMessage).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .OrderBy(t => t.Name);
        }

        private static byte[] SerializeWithFreshModel(object value)
        {
            using (var stream = new MemoryStream())
            {
                RuntimeTypeModel.Create().Serialize(stream, value);
                return stream.ToArray();
            }
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
