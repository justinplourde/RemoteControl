using System;

namespace MasterSplinter.Server.Core.RemoteDesktop
{
    public static class RemoteDesktopCoordinateMapper
    {
        public static bool TryMapZoomedPoint(
            int viewportWidth,
            int viewportHeight,
            int imageWidth,
            int imageHeight,
            int remoteWidth,
            int remoteHeight,
            int viewportX,
            int viewportY,
            out RemoteDesktopPoint remotePoint)
        {
            remotePoint = default;
            if (viewportWidth <= 0 || viewportHeight <= 0 ||
                imageWidth <= 0 || imageHeight <= 0 ||
                remoteWidth <= 0 || remoteHeight <= 0)
            {
                return false;
            }

            double scale = Math.Min(
                (double)viewportWidth / imageWidth,
                (double)viewportHeight / imageHeight);
            int renderedWidth = Math.Max(1, (int)Math.Round(imageWidth * scale));
            int renderedHeight = Math.Max(1, (int)Math.Round(imageHeight * scale));
            int offsetX = (viewportWidth - renderedWidth) / 2;
            int offsetY = (viewportHeight - renderedHeight) / 2;

            if (viewportX < offsetX ||
                viewportY < offsetY ||
                viewportX >= offsetX + renderedWidth ||
                viewportY >= offsetY + renderedHeight)
            {
                return false;
            }

            double normalizedX = (viewportX - offsetX) / (double)renderedWidth;
            double normalizedY = (viewportY - offsetY) / (double)renderedHeight;
            int remoteX = Clamp((int)Math.Floor(normalizedX * remoteWidth), 0, remoteWidth - 1);
            int remoteY = Clamp((int)Math.Floor(normalizedY * remoteHeight), 0, remoteHeight - 1);
            remotePoint = new RemoteDesktopPoint(remoteX, remoteY);
            return true;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;

            return value;
        }
    }
}
