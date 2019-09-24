using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using coderush.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace CarryTradeWebServer.Controllers
{
    public class TerminalController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public TerminalController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        // GET: Terminal
        public async Task<ActionResult> Index()
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            return View(user);
        }

        // GET: Terminal/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Terminal/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Terminal/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: Terminal/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Terminal/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: Terminal/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Terminal/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}