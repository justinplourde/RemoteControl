namespace MasterSplinter.Server.Core.RemoteDesktop
{
    public readonly struct RemoteDesktopPoint
    {
        public RemoteDesktopPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }

        public int Y { get; }
    }
}
