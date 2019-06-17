using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NiceTennisDenisCore.Models;

namespace NiceTennisDenisCore.Controllers
{
    /// <summary>
    /// <see cref="MatchPivot"/> controller.
    /// </summary>
    /// <seealso cref="Controller"/>
    [Produces("application/json")]
    [Route("api/Match")]
    public class MatchController : Controller
    {
        /// <summary>
        /// Gets Atp matches for a specified year.
        /// </summary>
        /// <param name="year">Year.</param>
        /// <param name="finalOnly">Final only y/n.</param>
        /// <returns>List of <see cref="MatchPivot"/>.</returns>
        [HttpGet("atp/{year}/{finalOnly}")]
        public List<MatchPivot> GetAtpMatchesForYear(uint year, bool finalOnly)
        {
            GlobalAppConfig.IsWtaContext = false;
            SqlMapper.LoadMatches(year, finalOnly);
            return MatchPivot.GetMatchesForAYear(year, finalOnly);
        }

        /// <summary>
        /// Gets Wta matches for a specified year.
        /// </summary>
        /// <param name="year">Year.</param>
        /// <param name="finalOnly">Final only y/n.</param>
        /// <returns>List of <see cref="MatchPivot"/>.</returns>
        [HttpGet("wta/{year}/{finalOnly}")]
        public List<MatchPivot> GetWtaMatchesForYear(uint year, bool finalOnly)
        {
            GlobalAppConfig.IsWtaContext = true;
            SqlMapper.LoadMatches(year, finalOnly);
            return MatchPivot.GetMatchesForAYear(year, finalOnly);
        }
    }
}