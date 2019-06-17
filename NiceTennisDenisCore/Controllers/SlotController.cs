using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NiceTennisDenisCore.Models;

namespace NiceTennisDenisCore.Controllers
{
    /// <summary>
    /// <see cref="SlotPivot"/> controller.
    /// </summary>
    /// <seealso cref="Controller"/>
    [Produces("application/json")]
    [Route("api/Slot")]
    public class SlotController : Controller
    {
        /// <summary>
        /// Gets every Wta slots.
        /// </summary>
        /// <returns>Collection of <see cref="SlotPivot"/>.</returns>
        [HttpGet("wta")]
        public List<SlotPivot> GetWta()
        {
            GlobalAppConfig.IsWtaContext = true;
            return SlotPivot.GetList();
        }

        /// <summary>
        /// Gets every Atp slots.
        /// </summary>
        /// <returns>Collection of <see cref="SlotPivot"/>.</returns>
        [HttpGet("atp")]
        public List<SlotPivot> GetAtp()
        {
            GlobalAppConfig.IsWtaContext = false;
            return SlotPivot.GetList();
        }
    }
}