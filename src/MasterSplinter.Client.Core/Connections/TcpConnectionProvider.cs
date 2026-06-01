using Quasar.Common.Enums;
using Quasar.Common.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;

namespace MasterSplinter.Client.Core.Connections
{
    public sealed class TcpConnectionProvider : IConnectionProvider
    {
        public TcpConnection[] GetConnections()
        {
            if (!OperatingSystem.IsWindows())
                return new TcpConnection[0];

            MibTcpRowOwnerPid[] table = GetTable();
            var connections = new TcpConnection[table.Length];

            for (int index = 0; index < table.Length; index++)
            {
                MibTcpRowOwnerPid row = table[index];
                connections[index] = new TcpConnection
                {
                    ProcessName = GetProcessName(row.OwningPid),
                    LocalAddress = row.LocalAddress.ToString(),
                    LocalPort = row.LocalPort,
                    RemoteAddress = row.RemoteAddress.ToString(),
                    RemotePort = row.RemotePort,
                    State = (ConnectionState)row.State
                };
            }

            return connections;
        }

        private static string GetProcessName(uint processId)
        {
            try
            {
                System.Diagnostics.Process process = System.Diagnostics.Process.GetProcessById((int)processId);
                return process.ProcessName;
            }
            catch
            {
                return $"PID: {processId}";
            }
        }

        private static MibTcpRowOwnerPid[] GetTable()
        {
            const int AfInet = 2;
            int bufferSize = 0;
            GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true, AfInet, TcpTableClass.TcpTableOwnerPidAll);
            if (bufferSize <= 0)
                return new MibTcpRowOwnerPid[0];

            IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
            try
            {
                uint result = GetExtendedTcpTable(buffer, ref bufferSize, true, AfInet, TcpTableClass.TcpTableOwnerPidAll);
                if (result != 0)
                    return new MibTcpRowOwnerPid[0];

                uint count = (uint)Marshal.ReadInt32(buffer);
                IntPtr rowPointer = IntPtr.Add(buffer, sizeof(uint));
                var rows = new List<MibTcpRowOwnerPid>((int)count);

                for (int index = 0; index < count; index++)
                {
                    var row = Marshal.PtrToStructure<MibTcpRowOwnerPid>(rowPointer);
                    rows.Add(row);
                    rowPointer = IntPtr.Add(rowPointer, Marshal.SizeOf<MibTcpRowOwnerPid>());
                }

                return rows.ToArray();
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(
            IntPtr pTcpTable,
            ref int dwOutBufLen,
            bool sort,
            int ipVersion,
            TcpTableClass tblClass,
            uint reserved = 0);

        [StructLayout(LayoutKind.Sequential)]
        private struct MibTcpRowOwnerPid
        {
            public uint State;
            public uint LocalAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] LocalPortBytes;
            public uint RemoteAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] RemotePortBytes;
            public uint OwningPid;

            public IPAddress LocalAddress => new IPAddress(LocalAddr);

            public ushort LocalPort => BitConverter.ToUInt16(new[] { LocalPortBytes[1], LocalPortBytes[0] }, 0);

            public IPAddress RemoteAddress => new IPAddress(RemoteAddr);

            public ushort RemotePort => BitConverter.ToUInt16(new[] { RemotePortBytes[1], RemotePortBytes[0] }, 0);
        }

        private enum TcpTableClass
        {
            TcpTableBasicListener,
            TcpTableBasicConnections,
            TcpTableBasicAll,
            TcpTableOwnerPidListener,
            TcpTableOwnerPidConnections,
            TcpTableOwnerPidAll,
            TcpTableOwnerModuleListener,
            TcpTableOwnerModuleConnections,
            TcpTableOwnerModuleAll
        }
    }
}
