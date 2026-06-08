using MasterSplinter.Server.Core.RemoteDesktop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MasterSplinter.Server.Core.Tests.RemoteDesktop
{
    [TestClass]
    public class RemoteDesktopCoordinateMapperTests
    {
        [TestMethod, TestCategory("ServerCore")]
        public void TryMapZoomedPointMapsUnletterboxedViewport()
        {
            bool mapped = RemoteDesktopCoordinateMapper.TryMapZoomedPoint(
                1280,
                720,
                1280,
                720,
                1280,
                720,
                640,
                360,
                out RemoteDesktopPoint point);

            Assert.IsTrue(mapped);
            Assert.AreEqual(640, point.X);
            Assert.AreEqual(360, point.Y);
        }

        [TestMethod, TestCategory("ServerCore")]
        public void TryMapZoomedPointAccountsForHorizontalLetterboxing()
        {
            bool mapped = RemoteDesktopCoordinateMapper.TryMapZoomedPoint(
                1000,
                500,
                800,
                500,
                1600,
                1000,
                500,
                250,
                out RemoteDesktopPoint point);

            Assert.IsTrue(mapped);
            Assert.AreEqual(800, point.X);
            Assert.AreEqual(500, point.Y);
        }

        [TestMethod, TestCategory("ServerCore")]
        public void TryMapZoomedPointRejectsLetterboxArea()
        {
            bool mapped = RemoteDesktopCoordinateMapper.TryMapZoomedPoint(
                1000,
                500,
                800,
                500,
                1600,
                1000,
                25,
                250,
                out _);

            Assert.IsFalse(mapped);
        }

        [TestMethod, TestCategory("ServerCore")]
        public void TryMapZoomedPointClampsLowerRightEdge()
        {
            bool mapped = RemoteDesktopCoordinateMapper.TryMapZoomedPoint(
                100,
                100,
                100,
                100,
                1920,
                1080,
                99,
                99,
                out RemoteDesktopPoint point);

            Assert.IsTrue(mapped);
            Assert.AreEqual(1900, point.X);
            Assert.AreEqual(1069, point.Y);
        }

        [TestMethod, TestCategory("ServerCore")]
        public void TryMapZoomedPointRejectsInvalidDimensions()
        {
            bool mapped = RemoteDesktopCoordinateMapper.TryMapZoomedPoint(
                0,
                100,
                100,
                100,
                100,
                100,
                10,
                10,
                out _);

            Assert.IsFalse(mapped);
        }
    }
}
