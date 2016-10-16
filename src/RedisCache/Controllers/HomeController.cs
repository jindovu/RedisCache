using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using RedisCache.Cache;
using RedisCache.ViewModels;

namespace RedisCache.Controllers
{
    public class HomeController : Controller
    {
        private readonly ICacheService _cacheService;
        public HomeController(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        public IActionResult Index()
        {
            var result = _cacheService.Get<List<TestViewModel>>("strKey");
            if (result == null)
            {
                result = GetListTest();
                _cacheService.Store("strKey", result);
            }

            return View(result);
        }

        private List<TestViewModel> GetListTest()
        {
            var listTest = new List<TestViewModel>();
            for (int i = 0; i < 10000; i++)
            {
                listTest.Add(new TestViewModel { No = i, Name = "Test "+ i, Content = "Test content "+ i, Date = DateTime.Now });
            }
            return listTest;
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
