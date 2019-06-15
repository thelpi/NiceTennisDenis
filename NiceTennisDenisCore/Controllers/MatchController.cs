using System.Collections.Generic;
using System.Linq;
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
        /// <returns>List of <see cref="MatchPivot"/>.</returns>
        [HttpGet("atp/{year}")]
        public List<MatchPivot> GetAtpMatchesForYear(uint year)
        {
            GlobalAppConfig.IsWtaContext = false;
            SqlMapper.LoadMatches(year);
            return MatchPivot.GetList().Where(m => m.Edition.Year == year).ToList();
        }

        /// <summary>
        /// Gets Wta matches for a specified year.
        /// </summary>
        /// <param name="year">Year.</param>
        /// <returns>List of <see cref="MatchPivot"/>.</returns>
        [HttpGet("wta/{year}")]
        public List<MatchPivot> GetWtaMatchesForYear(uint year)
        {
            GlobalAppConfig.IsWtaContext = true;
            SqlMapper.LoadMatches(year);
            return MatchPivot.GetList().Where(m => m.Edition.Year == year).ToList();
        }
    }
}