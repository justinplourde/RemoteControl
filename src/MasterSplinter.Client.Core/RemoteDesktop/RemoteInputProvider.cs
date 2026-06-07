using MasterSplinter.Common.Enums;
using MasterSplinter.Common.Messages;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MasterSplinter.Client.Core.RemoteDesktop
{
#pragma warning disable CA1416
    public sealed class RemoteInputProvider : IRemoteInputProvider
    {
        private const int InputMouse = 0;
        private const int InputKeyboard = 1;
        private const uint MouseEventLeftDown = 0x0002;
        private const uint MouseEventLeftUp = 0x0004;
        private const uint MouseEventRightDown = 0x0008;
        private const uint MouseEventRightUp = 0x0010;
        private const uint MouseEventWheel = 0x0800;
        private const uint KeyEventKeyDown = 0x0000;
        private const uint KeyEventKeyUp = 0x0002;

        public RemoteInputResult SendMouseEvent(DoMouseEvent mouseEvent)
        {
            if (!OperatingSystem.IsWindows())
                return RemoteInputResult.Error("Remote input is only supported on Windows.");
            if (mouseEvent == null)
                return RemoteInputResult.Error("Mouse event is required.");

            try
            {
                MonitorBounds bounds = GetMonitorBounds(mouseEvent.MonitorIndex);
                int x = bounds.X + mouseEvent.X;
                int y = bounds.Y + mouseEvent.Y;

                switch (mouseEvent.Action)
                {
                    case MouseAction.LeftDown:
                    case MouseAction.LeftUp:
                        return SendMouseButton(x, y, mouseEvent.IsMouseDown ? MouseEventLeftDown : MouseEventLeftUp);
                    case MouseAction.RightDown:
                    case MouseAction.RightUp:
                        return SendMouseButton(x, y, mouseEvent.IsMouseDown ? MouseEventRightDown : MouseEventRightUp);
                    case MouseAction.MoveCursor:
                        return SetCursorPos(x, y) ? RemoteInputResult.Success() : RemoteInputResult.Error("SetCursorPos failed.");
                    case MouseAction.ScrollDown:
                        return SendMouseWheel(x, y, -120);
                    case MouseAction.ScrollUp:
                        return SendMouseWheel(x, y, 120);
                    case MouseAction.None:
                        return RemoteInputResult.Success();
                    default:
                        return RemoteInputResult.Error("Unsupported mouse action.");
                }
            }
            catch (Exception exception)
            {
                return RemoteInputResult.Error(exception.Message);
            }
        }

        public RemoteInputResult SendKeyboardEvent(DoKeyboardEvent keyboardEvent)
        {
            if (!OperatingSystem.IsWindows())
                return RemoteInputResult.Error("Remote input is only supported on Windows.");
            if (keyboardEvent == null)
                return RemoteInputResult.Error("Keyboard event is required.");

            try
            {
                var input = new INPUT
                {
                    type = InputKeyboard,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = keyboardEvent.Key,
                            wScan = 0,
                            dwFlags = keyboardEvent.KeyDown ? KeyEventKeyDown : KeyEventKeyUp,
                            time = 0,
                            dwExtraInfo = GetMessageExtraInfo()
                        }
                    }
                };

                return SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT))) == 1
                    ? RemoteInputResult.Success()
                    : RemoteInputResult.Error("SendInput failed.");
            }
            catch (Exception exception)
            {
                return RemoteInputResult.Error(exception.Message);
            }
        }

        private static RemoteInputResult SendMouseButton(int x, int y, uint flags)
        {
            SetCursorPos(x, y);
            return SendMouseInput(x, y, 0, flags);
        }

        private static RemoteInputResult SendMouseWheel(int x, int y, int wheelDelta)
        {
            SetCursorPos(x, y);
            return SendMouseInput(x, y, wheelDelta, MouseEventWheel);
        }

        private static RemoteInputResult SendMouseInput(int x, int y, int mouseData, uint flags)
        {
            var input = new INPUT
            {
                type = InputMouse,
                u = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dx = x,
                        dy = y,
                        mouseData = mouseData,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = GetMessageExtraInfo()
                    }
                }
            };

            return SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT))) == 1
                ? RemoteInputResult.Success()
                : RemoteInputResult.Error("SendInput failed.");
        }

        private static MonitorBounds GetMonitorBounds(int monitorIndex)
        {
            if (monitorIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(monitorIndex), "Monitor index must be zero or greater.");

            var monitors = new List<MonitorBounds>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr monitor, IntPtr hdc, ref RECT rect, IntPtr data) =>
            {
                monitors.Add(new MonitorBounds(rect.Left, rect.Top));
                return true;
            }, IntPtr.Zero);

            if (monitorIndex >= monitors.Count)
                throw new ArgumentOutOfRangeException(nameof(monitorIndex), "Monitor index is out of range.");

            return monitors[monitorIndex];
        }

        private readonly struct MonitorBounds
        {
            public MonitorBounds(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X { get; }

            public int Y { get; }
        }

        private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, ref RECT rect, IntPtr data);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern IntPtr GetMessageExtraInfo();

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint inputsCount, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] inputs, int size);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;

            [FieldOffset(0)]
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public int mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
    }
#pragma warning restore CA1416
}
