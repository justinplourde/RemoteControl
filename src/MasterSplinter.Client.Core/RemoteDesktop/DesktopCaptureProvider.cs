using MasterSplinter.Common.Messages;
using MasterSplinter.Common.Video;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace MasterSplinter.Client.Core.RemoteDesktop
{
#pragma warning disable CA1416
    public sealed class DesktopCaptureProvider : IDesktopCaptureProvider
    {
        public GetDesktopResponse Capture(GetDesktop request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            int quality = ClampQuality(request.Quality);
            int displayIndex = Math.Max(0, request.DisplayIndex);

            if (!OperatingSystem.IsWindows())
            {
                return new GetDesktopResponse
                {
                    Image = null,
                    Quality = quality,
                    Monitor = displayIndex,
                    Resolution = new Resolution()
                };
            }

            MonitorBounds bounds;
            try
            {
                bounds = GetMonitorBounds(displayIndex);
            }
            catch
            {
                return new GetDesktopResponse
                {
                    Image = null,
                    Quality = quality,
                    Monitor = displayIndex,
                    Resolution = new Resolution()
                };
            }

            var resolution = new Resolution { Width = bounds.Width, Height = bounds.Height };
            try
            {
                using (var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb))
                {
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);
                    }

                    byte[] jpeg = EncodeJpeg(bitmap, quality);
                    byte[] legacyFirstFrame = new byte[jpeg.Length + 4];
                    Buffer.BlockCopy(BitConverter.GetBytes(jpeg.Length), 0, legacyFirstFrame, 0, 4);
                    Buffer.BlockCopy(jpeg, 0, legacyFirstFrame, 4, jpeg.Length);

                    return new GetDesktopResponse
                    {
                        Image = legacyFirstFrame,
                        Quality = quality,
                        Monitor = displayIndex,
                        Resolution = resolution
                    };
                }
            }
            catch
            {
                return new GetDesktopResponse
                {
                    Image = null,
                    Quality = quality,
                    Monitor = displayIndex,
                    Resolution = resolution
                };
            }
        }

        private static int ClampQuality(int quality)
        {
            if (quality <= 0)
                return 75;
            if (quality > 100)
                return 100;

            return quality;
        }

        private static byte[] EncodeJpeg(Bitmap bitmap, int quality)
        {
            ImageCodecInfo codec = ImageCodecInfo.GetImageEncoders()
                .FirstOrDefault(encoder => string.Equals(encoder.MimeType, "image/jpeg", StringComparison.OrdinalIgnoreCase));
            if (codec == null)
                throw new InvalidOperationException("JPEG encoder was not found.");

            using (var parameters = new EncoderParameters(1))
            {
                parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, codec, parameters);
                    return stream.ToArray();
                }
            }
        }

        private static MonitorBounds GetMonitorBounds(int monitorIndex)
        {
            var monitors = new List<MonitorBounds>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr monitor, IntPtr hdc, ref RECT rect, IntPtr data) =>
            {
                monitors.Add(new MonitorBounds(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top));
                return true;
            }, IntPtr.Zero);

            if (monitorIndex < 0 || monitorIndex >= monitors.Count)
                throw new ArgumentOutOfRangeException(nameof(monitorIndex), "Monitor index is out of range.");

            return monitors[monitorIndex];
        }

        private readonly struct MonitorBounds
        {
            public MonitorBounds(int x, int y, int width, int height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }

            public int X { get; }

            public int Y { get; }

            public int Width { get; }

            public int Height { get; }
        }

        private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, ref RECT rect, IntPtr data);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
#pragma warning restore CA1416
}
