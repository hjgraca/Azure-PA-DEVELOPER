// ----------------------------------------------------------------------------------
// Microsoft Developer & Platform Evangelism
// 
// Copyright (c) Microsoft Corporation. All rights reserved.
// 
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
// OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
// The example companies, organizations, products, domain names,
// e-mail addresses, logos, people, places, and events depicted
// herein are fictitious.  No association with any real company,
// organization, product, domain name, email address, logo, person,
// places, or events is intended or should be inferred.
// ----------------------------------------------------------------------------------
#define TRACE
namespace CloudShop.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using CloudShop.Models;
    using System.Configuration;
    using System.Diagnostics;

    [HandleError]
    public class HomeController : Controller
    {
        public ActionResult About()
        {
            return View();
        }
        private void Diagnostic()
        {
            ViewBag.Message = Environment.MachineName;

            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            int start = connectionString.IndexOf(";Password=");
            var end = connectionString.IndexOf(";", start + 1);
            var connectionStringSafe = connectionString.Remove(start, end - start);
            ViewBag.ConnectionString = connectionStringSafe;

            string shopName = ConfigurationManager.AppSettings["CloudShopName"].ToString();


            System.Diagnostics.Trace.TraceWarning("App Setting CloudShopName has value " + shopName + ".");
            ViewBag.Title = shopName;
        }
        public ActionResult Index()
        {

            Diagnostic();
            System.Diagnostics.Trace.TraceError("HomeController Index()-TraceError trace output.");
            //show all products
            return Search(null);
        }

        [HttpPost]
        public ActionResult Add(string selectedProduct)
        {
            if (selectedProduct != null)
            {
                List<string> cart = this.Session["Cart"] as List<string> ?? new List<string>();
                cart.Add(selectedProduct);
                Session["Cart"] = cart;
                System.Diagnostics.Trace.TraceInformation("HomeController TraceInformation - Add()- product " + selectedProduct+ " added to cart");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Search(string SearchCriteria)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            Services.IProductRepository productRepository = new Services.ProductsRepository();
            var products = string.IsNullOrEmpty(SearchCriteria) ?
                productRepository.GetProducts() : productRepository.Search(SearchCriteria);

            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;

            string elapsedTime = String.Format("{0:00}.{1:000} sec", ts.Seconds, ts.Milliseconds);


            // add all products currently not in session
            var itemsInSession = this.Session["Cart"] as List<string> ?? new List<string>();
            var filteredProducts = products.Where(item => !itemsInSession.Contains(item));

            var model = new IndexViewModel()
            {
                Products = filteredProducts,
                SearchCriteria = SearchCriteria
            };
            Diagnostic();
            ViewBag.RepoTime = elapsedTime;
            return View("Index", model);
        }

        public ActionResult Checkout()
        {
            ViewBag.Message = Environment.MachineName;
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            int start = connectionString.IndexOf(";Password=");
            var end = connectionString.IndexOf(";", start + 1);
            var connectionStringSafe = connectionString.Remove(start, end - start);
            ViewBag.ConnectionString = connectionStringSafe;

            var itemsInSession = this.Session["Cart"] as List<string> ?? new List<string>();
            var model = new IndexViewModel()
            {
                Products = itemsInSession
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult Remove(string Products)
        {
            if (Products != null)
            {
                var itemsInSession = this.Session["Cart"] as List<string>;
                if (itemsInSession != null)
                {
                    itemsInSession.Remove(Products);
                }
            }

            return RedirectToAction("Checkout");
        }
    }
}