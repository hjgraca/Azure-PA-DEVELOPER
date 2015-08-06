/*
    Copyright 2014 Microsoft, Corp.

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

        http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
*/

using Microsoft.Azure.SqlDatabase.ElasticScale.Query;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

////////////////////////////////////////////////////////////////////////////////////////
// This sample follows the CodeFirstNewDatabase Blogging tutorial for EF.
// It illustrates the adjustments that need to be made to use EF in combination
// with the Entity Framewor to scale out your data tier across many databases and
// benefit from Elastic Scale capabilities for Data Dependent Routing and 
// Shard Map Management.
////////////////////////////////////////////////////////////////////////////////////////
namespace EFCodeFirstElasticScale
{
    // This sample requires three pre-created empty SQL Server databases. 
    // The first database serves as the shard map manager database to store the Elastic Scale shard map.
    // The remaining two databases serve as shards to hold the data for the sample.
    internal class Program
    {
        // You need to adjust the following settings to your database server and database names in Azure Db
        private static string server = "eu-west-gps-v12.database.windows.net";
        private static string shardmapmgrdb = "ShardMgtDB";
        private static string shard1 = "DB1";
        private static string shard2 = "DB2";
        private static string userName = "demouser";
        private static string password = "P@ssw0rd1!?";
        private static string applicationName = "ESC_EFv1.0";

        // Just two tenants for now.
        // Those we will allocate to shards.
        private static int tenantId1 = 42;
        private static int tenantId2 = 12;

        public static void Main()
        {
            SqlConnectionStringBuilder connStrBldr = new SqlConnectionStringBuilder
            {
                UserID = userName,
                Password = password,
                ApplicationName = applicationName
            };

            // Bootstrap the shard map manager, register shards, and store mappings of tenants to shards
            // Note that you can keep working with existing shard maps. There is no need to 
            // re-create and populate the shard map from scratch every time.
            Console.WriteLine("Checking for existing shard map and creating new shard map if necessary.");
            
            // our shard map manager db is ShardMgtDB
            Sharding sharding = new Sharding(server, shardmapmgrdb, connStrBldr.ConnectionString);
            sharding.RegisterNewShard(server, shard1, connStrBldr.ConnectionString, tenantId1);
            sharding.RegisterNewShard(server, shard2, connStrBldr.ConnectionString, tenantId2);

            // Do work for tenant 1 :-)

            // Create and save a new Blog 
            Console.Write("Enter a name for a new Blog: ");
            var name = Console.ReadLine();

            SqlDatabaseUtils.SqlRetryPolicy.ExecuteAction(() =>
            {
                using (var db = new ElasticScaleContext<int>(sharding.ShardMap, tenantId1, connStrBldr.ConnectionString))
                {
                    var blog = new Blog { Name = name };
                    db.Blogs.Add(blog);
                    db.SaveChanges();
                }
            });

            SqlDatabaseUtils.SqlRetryPolicy.ExecuteAction(() =>
            {
                using (var db = new ElasticScaleContext<int>(sharding.ShardMap, tenantId1, connStrBldr.ConnectionString))
                {    
                    // Display all Blogs for tenant 1
                    var query = from b in db.Blogs
                                orderby b.Name
                                select b;

                    Console.WriteLine("****** All blogs for tenant id {0}:", tenantId1);
                    foreach (var item in query)
                    {
                        Console.WriteLine(item.Name);
                    }
                }
            });

            Console.WriteLine();
            // Do work for tenant 2 :-)
            SqlDatabaseUtils.SqlRetryPolicy.ExecuteAction(() =>
            {
                using (var db = new ElasticScaleContext<int>(sharding.ShardMap, tenantId2, connStrBldr.ConnectionString))
                {
                    // Display all Blogs from the database 
                    var query = from b in db.Blogs
                                orderby b.Name
                                select b;

                    Console.WriteLine("****** All blogs for tenant id {0}:", tenantId2);
                    foreach (var item in query)
                    {
                        Console.WriteLine(item.Name);
                    }
                }
            });

            // Create and save a new Blog 
            Console.Write("Enter a name for a new Blog: ");
            var name2 = Console.ReadLine();

            SqlDatabaseUtils.SqlRetryPolicy.ExecuteAction(() =>
            {
                using (var db = new ElasticScaleContext<int>(sharding.ShardMap, tenantId2, connStrBldr.ConnectionString))
                {
                    var blog = new Blog { Name = name2 };
                    db.Blogs.Add(blog);
                    db.SaveChanges();
                }
            });

            SqlDatabaseUtils.SqlRetryPolicy.ExecuteAction(() =>
            {
                using (var db = new ElasticScaleContext<int>(sharding.ShardMap, tenantId2, connStrBldr.ConnectionString))
                {                    
                    // Display all Blogs from the database 
                    var query = from b in db.Blogs
                            orderby b.Name
                            select b;

                    Console.WriteLine("****** All blogs for tenant id {0}:", tenantId2);
                    foreach (var item in query)
                    {
                        Console.WriteLine(item.Name);
                    }
                }
            });
            
            // Multi shard query
            Console.WriteLine("****** Query accross all shards for blog titles ***** ");
            SqlDatabaseUtils.SqlRetryPolicy.ExecuteAction(() =>
            {
                using (MultiShardConnection conn = new MultiShardConnection(
                                    sharding.ShardMap.GetShards(),
                                    connStrBldr.ConnectionString))
                {
                    using (MultiShardCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT Name FROM Blogs";
                        cmd.CommandType = CommandType.Text;
                        cmd.ExecutionOptions = MultiShardExecutionOptions.IncludeShardNameColumn;
                        cmd.ExecutionPolicy = MultiShardExecutionPolicy.PartialResults;

                        using (MultiShardDataReader sdr = cmd.ExecuteReader())
                        {
                            while (sdr.Read())
                            {
                                var title = sdr.GetString(0);
                                Console.WriteLine(title);
                            }
                        }
                    }
                }
            });
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }
    }
}
