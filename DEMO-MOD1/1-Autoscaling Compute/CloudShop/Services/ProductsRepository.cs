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

using System.Collections.Generic;
using System.Linq;
using CloudShop.Models;

namespace CloudShop.Services
{
    public class ProductsRepository : IProductRepository
    {
        public List<string> GetProducts()
        {
            List<string> products = null;

            AdventureWorksEntities context = new AdventureWorksEntities();
            var query = from product in context.Products
                        select product.Name;
            products = query.ToList();

            return products;
        }

        public List<string> Search(string criteria)
        {
            var  context = new AdventureWorksEntities();
            
            var query = context.Database.SqlQuery<string>("Select Name from SalesLT.Product Where Freetext(*,{0})", 
                criteria);

            return query.ToList();
            
      
        }

    }
}