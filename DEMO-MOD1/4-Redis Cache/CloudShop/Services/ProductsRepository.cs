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
using StackExchange.Redis;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace CloudShop.Services
{
    #region "Redis cache extension"
    public static class SampleStackExchangeRedisExtensions
    {
        public static T Get<T>(this IDatabase cache, string key)
        {
            return Deserialize<T>(cache.StringGet(key));
        }

        public static object Get(this IDatabase cache, string key)
        {
            return Deserialize<object>(cache.StringGet(key));
        }

        public static void Set(this IDatabase cache, string key, object value)
        {
            cache.StringSet(key, Serialize(value));
        }

        public static void Set(this IDatabase cache, string key, object value, TimeSpan span)
        {
            cache.StringSet(key, Serialize(value), span);
        }

        static byte[] Serialize(object o)
        {
            if (o == null)
            {
                return null;
            }

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, o);
                byte[] objectDataAsStream = memoryStream.ToArray();
                return objectDataAsStream;
            }
        }

        static T Deserialize<T>(byte[] stream)
        {
            if (stream == null)
            {
                return default(T);
            }

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream(stream))
            {
                T result = (T)binaryFormatter.Deserialize(memoryStream);
                return result;
            }
        }
    }
    #endregion
    public class ProductsRepository : IProductRepository
    {
        /*
        public List<string> GetProducts()
        {
            List<string> products = null;

            AdventureWorksEntities context = new AdventureWorksEntities();
            var query = from product in context.Products
                        select product.Name;
            products = query.ToList();

            return products;
        }
        */
        public List<string> GetProducts()
        {

            List<string> products = null;
            IDatabase cache = MvcApplication.RedisCache.GetDatabase();
            products = cache.Get<List<string>>("PRODUCTS");
            if (products == null)
            {
                AdventureWorksEntities context = new AdventureWorksEntities();
                var query = from product in context.Products
                            select product.Name;
                products = query.ToList();
                cache.Set("PRODUCTS", products, TimeSpan.FromMinutes(5));
            }
            return products;
        }
        /*
        public List<string> Search(string criteria)
        {
            var  context = new AdventureWorksEntities();
            
            var query = context.Database.SqlQuery<string>("Select Name from SalesLT.Product Where Freetext(*,{0})", 
                criteria);

            return query.ToList();
            
      
        }
        */

        public List<string> Search(string criteria)
        {
            List<string> products = null;
            IDatabase cache = MvcApplication.RedisCache.GetDatabase();
            products = cache.Get<List<string>>("QUERY-"+criteria);
            if (products == null)
            {
                var context = new AdventureWorksEntities();

                var query = context.Database.SqlQuery<string>("Select Name from SalesLT.Product Where Freetext(*,{0})",
                    criteria);

                products = query.ToList();
                cache.Set("QUERY-" + criteria, products, TimeSpan.FromMinutes(5));
            }
            return products;

        }

    }
}