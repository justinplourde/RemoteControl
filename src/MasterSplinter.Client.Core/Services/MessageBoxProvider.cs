using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace MasterSplinter.Client.Core.Services
{
    public sealed class MessageBoxProvider : IMessageBoxProvider
    {
        private const uint DefaultDesktopOnly = 0x00020000;

        public void Show(string text, string caption, string button, string icon)
        {
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Message boxes are only supported on Windows.");

            var thread = new Thread(() => MessageBox(IntPtr.Zero, text ?? string.Empty, caption ?? string.Empty, GetFlags(button, icon)))
            {
                IsBackground = true
            };
            thread.Start();
        }

        private static uint GetFlags(string button, string icon)
        {
            return GetButtonFlag(button) | GetIconFlag(icon) | DefaultDesktopOnly;
        }

        private static uint GetButtonFlag(string button)
        {
            switch (button)
            {
                case "AbortRetryIgnore":
                    return 0x00000002;
                case "OK":
                    return 0x00000000;
                case "OKCancel":
                    return 0x00000001;
                case "RetryCancel":
                    return 0x00000005;
                case "YesNo":
                    return 0x00000004;
                case "YesNoCancel":
                    return 0x00000003;
                default:
                    return 0x00000000;
            }
        }

        private static uint GetIconFlag(string icon)
        {
            switch (icon)
            {
                case "Asterisk":
                case "Information":
                    return 0x00000040;
                case "Error":
                case "Hand":
                    return 0x00000010;
                case "Exclamation":
                case "Warning":
                    return 0x00000030;
                case "Question":
                    return 0x00000020;
                case "None":
                default:
                    return 0x00000000;
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
    }
}
