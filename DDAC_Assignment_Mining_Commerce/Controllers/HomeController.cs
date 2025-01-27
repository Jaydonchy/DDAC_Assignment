﻿using DDAC_Assignment_Mining_Commerce.Helper;
using DDAC_Assignment_Mining_Commerce.Models;
using DDAC_Assignment_Mining_Commerce.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DDAC_Assignment_Mining_Commerce.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TableService _tableService;
        private readonly MiningCommerceContext _context;
        public HomeController(ILogger<HomeController> logger, TableService tableService, MiningCommerceContext context)
        {
            _logger = logger;
            _context = context;
            _tableService = tableService;
        }


        public async Task<IActionResult> Index()
        {
            return View("../DisplayProducts/Display", await _context.Product.ToListAsync());
        }

        public IActionResult About()
        {
            return View();
        }

        public async Task ClearNotificationsAsync()
        {
            string buyerID = HttpContext.Session.Get<BuyerModel>("AuthRole").ID.ToString();
            await _tableService.DeleteNotificationsByRK(buyerID);
            List<Notification> notifications = await _tableService.GetNotificationsByRK(buyerID);
            HttpContext.Session.Set<IEnumerable<Notification>>("Notifications", notifications);
        }

        public async Task UpdateNotificationsAsync()
        {
            BuyerModel buyer = HttpContext.Session.Get<BuyerModel>("AuthRole");
            if (buyer != null)
            {
                IEnumerable<Notification> notifications = await _tableService.GetNotificationsByRK(buyer.ID.ToString());
                HttpContext.Session.Set("Notifications", notifications);
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
