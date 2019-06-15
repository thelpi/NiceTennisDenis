using System;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NiceTennisDenisIntegrationTests
{
    [TestClass]
    public class RankingTests
    {
        private Tuple<uint, uint> Request(uint versionId, DateTime date, uint playerId)
        {
            var request = (HttpWebRequest)WebRequest.Create($"http://localhost:52368/api/Ranking/atp/debug/{versionId}/{date.ToString("yyyy-MM-dd")}/{playerId}");
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                using (var responseStream = response.GetResponseStream())
                {
                    using (var streamReader = new System.IO.StreamReader(responseStream))
                    {
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<Tuple<uint, uint>>(streamReader.ReadToEnd());
                    }
                }
            }
        }

        [TestMethod]
        public void DebugRankingForPlayer_DelPotro_2018_December()
        {
            var result = Request(2, new DateTime(2018, 12, 24), 105223);

            Assert.IsNotNull(result);
            Assert.AreEqual((uint)5300, result.Item1);
            Assert.AreEqual((uint)15, result.Item2);
        }

        [TestMethod]
        public void DebugRankingForPlayer_Dimitrov_2017_December()
        {
            var result = Request(2, new DateTime(2017, 12, 25), 105777);

            Assert.IsNotNull(result);
            Assert.AreEqual((uint)5150, result.Item1);
            Assert.AreEqual((uint)23, result.Item2);
        }
    }
}
