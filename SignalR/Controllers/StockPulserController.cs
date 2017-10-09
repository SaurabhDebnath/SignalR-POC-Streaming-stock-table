using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SignalR.Controllers
{
    public class StockPulserController : Controller
    {
        // GET: StockPulser
        public ActionResult Index()
        {
            return View();
        }
    }
}