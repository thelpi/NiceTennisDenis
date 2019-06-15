using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NiceTennisDenisCore.Models;

namespace NiceTennisDenisCore.Controllers
{
    /// <summary>
    /// <see cref="EditionPivot"/> controller.
    /// </summary>
    /// <seealso cref="Controller"/>
    [Produces("application/json")]
    [Route("api/Edition")]
    public class EditionController : Controller
    {
        /// <summary>
        /// Gets every Wta editions from a year to another.
        /// </summary>
        /// <param name="yearBegin">Included first year.</param>
        /// <param name="yearEnd">Included last year.</param>
        /// <returns>Collection of <see cref="EditionPivot"/>.</returns>
        [HttpGet("wta/{yearBegin}/{yearEnd}")]
        public IReadOnlyCollection<EditionPivot> GetWtaFromYearToYear(uint yearBegin, uint yearEnd)
        {
            GlobalAppConfig.IsWtaContext = true;
            return EditionPivot.GetList().Where(e => e.Year >= yearBegin && e.Year <= yearEnd).ToList();
        }

        /// <summary>
        /// Gets every Atp editions from a year to another.
        /// </summary>
        /// <param name="yearBegin">Included first year.</param>
        /// <param name="yearEnd">Included last year.</param>
        /// <returns>Collection of <see cref="EditionPivot"/>.</returns>
        [HttpGet("atp/{yearBegin}/{yearEnd}")]
        public IReadOnlyCollection<EditionPivot> GetAtpFromYearToYear(uint yearBegin, uint yearEnd)
        {
            GlobalAppConfig.IsWtaContext = false;
            return EditionPivot.GetList().Where(e => e.Year >= yearBegin && e.Year <= yearEnd).ToList();
        }
    }
}