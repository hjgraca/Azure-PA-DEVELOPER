﻿/*
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

using System.Data.SqlClient;
using System.Linq;
using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement;

namespace EFCodeFirstElasticScale
{
    internal class Sharding
    {
        public ShardMapManager ShardMapManager { get; private set; }

        public ListShardMap<int> ShardMap { get; private set; }

        // Bootstrap Elastic Scale by creating a new shard map manager and a shard map on 
        // the shard map manager database if necessary.
        public Sharding(string smmserver, string smmdatabase, string smmconnstr)
        {
            // Connection string with administrative credentials for the root database
            SqlConnectionStringBuilder connStrBldr = new SqlConnectionStringBuilder(smmconnstr);
            connStrBldr.DataSource = smmserver;
            connStrBldr.InitialCatalog = smmdatabase;

            // Deploy shard map manager.
            ShardMapManager smm;
            if (!ShardMapManagerFactory.TryGetSqlShardMapManager(connStrBldr.ConnectionString, ShardMapManagerLoadPolicy.Lazy, out smm))
            {
                this.ShardMapManager = ShardMapManagerFactory.CreateSqlShardMapManager(connStrBldr.ConnectionString);
            }
            else
            {
                this.ShardMapManager = smm;
            }

            ListShardMap<int> sm;
            if (!ShardMapManager.TryGetListShardMap<int>("ElasticScaleWithEF", out sm))
            {
                this.ShardMap = ShardMapManager.CreateListShardMap<int>("ElasticScaleWithEF");
            }
            else
            {
                this.ShardMap = sm;
            }
        }

        // Enter a new shard - i.e. an empty database - to the shard map, allocate a first tenant to it 
        // and kick off EF intialization of the database to deploy schema
        // public void RegisterNewShard(string server, string database, string user, string pwd, string appname, int key)
        public void RegisterNewShard(string server, string database, string connstr, int key)
        {
            Shard shard;
            ShardLocation shardLocation = new ShardLocation(server, database);

            if (!this.ShardMap.TryGetShard(shardLocation, out shard))
            {
                shard = this.ShardMap.CreateShard(shardLocation);
            }
            
            SqlConnectionStringBuilder connStrBldr = new SqlConnectionStringBuilder(connstr);
            connStrBldr.DataSource = server;
            connStrBldr.InitialCatalog = database;

            // Go into a DbContext to trigger migrations and schema deployment for the new shard.
            // This requires an un-opened connection.
            using (var db = new ElasticScaleContext<int>(connStrBldr.ConnectionString))
            {
                // Run a query to engage EF migrations
                (from b in db.Blogs
                    select b).Count();
            }

            // Register the mapping of the tenant to the shard in the shard map.
            // After this step, DDR on the shard map can be used
            PointMapping<int> mapping;
            if (!this.ShardMap.TryGetMappingForKey(key, out mapping))
            {
                this.ShardMap.CreatePointMapping(key, shard);
            }
        }
    }
}