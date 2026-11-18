using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Privatni_casovi.Models;
using System.Diagnostics;

namespace Privatni_casovi.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// The landing page — one of only three endpoints an anonymous visitor may reach,
        /// alongside registration and prijava.
        /// </summary>
        /// <remarks>
        /// A signed-in korisnik does not have a landing page: their home is the pretraga, so
        /// this hands them straight to it. That is what makes the pretraga "the home page
        /// after login" without <c>AccountController</c> having to know about it — prijava
        /// returns to <c>/</c>, and so do the brand and the "Početna" link.
        /// </remarks>
        [AllowAnonymous]
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(PretragaController.Index), "Pretraga");
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // Anonymous so that a failure while signed out renders the error page instead of
        // bouncing through the login redirect.
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
