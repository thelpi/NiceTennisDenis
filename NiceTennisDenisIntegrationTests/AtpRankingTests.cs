using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiceTennisDenisIntegrationTests.Properties;

namespace NiceTennisDenisIntegrationTests
{
    [TestClass]
    public class AtpRankingTests
    {
        [TestMethod]
        public void DebugAtpRankingForPlayer_DelPotro_2018_December()
        {
            Initialize();
            var result = NiceTennisDenisDll.DataMapper.Default.DebugAtpRankingForPlayer(105223, 2, new DateTime(2018, 12, 24));
            Assert.IsNotNull(result);
            Assert.AreEqual((uint)5300, result.Item1);
            Assert.AreEqual((uint)15, result.Item2);
        }

        [TestMethod]
        public void DebugAtpRankingForPlayer_Dimitrov_2017_December()
        {
            Initialize();
            var result = NiceTennisDenisDll.DataMapper.Default.DebugAtpRankingForPlayer(105777, 2, new DateTime(2017, 12, 25));
            Assert.IsNotNull(result);
            Assert.AreEqual((uint)5150, result.Item1);
            Assert.AreEqual((uint)23, result.Item2);
        }

        private void Initialize()
        {
            NiceTennisDenisDll.DataMapper.InitializeDefault(
                string.Format(Settings.Default.sqlConnStringPattern,
                    Settings.Default.sqlServer,
                    Settings.Default.sqlDatabase,
                    Settings.Default.sqlUser,
                    Settings.Default.sqlPassword
                ), Settings.Default.datasDirectory).LoadModel();
        }
    }
}
