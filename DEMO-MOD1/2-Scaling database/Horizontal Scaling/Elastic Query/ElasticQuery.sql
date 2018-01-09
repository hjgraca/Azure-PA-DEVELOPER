https://azure.microsoft.com/en-us/documentation/articles/sql-database-elastic-query-getting-started/

-- However here we use a
-- Create a db master key if one does not already exist, using your own password.
CREATE MASTER KEY ENCRYPTION BY PASSWORD='RedW1ne!';

-- Create a database credential
-- Note – this syntax is temporary and will be changed in the next release.
CREATE CREDENTIAL cred ON DATABASE WITH IDENTITY = 'morcinim', SECRET='RedW1ne!';
-- For high security, drop the credential when it is no longer in use. 
-- Note, this syntax is temporary and will be changed in the next release.
-- DROP CREDENTIAL cred ON DATABASE;

-- DROP EXTERNAL DATA SOURCE AllBlogsDBs
CREATE EXTERNAL DATA SOURCE AllBlogsDBs
WITH
(
TYPE=SHARD_MAP_MANAGER,
LOCATION='eu-west-gps-v12.database.windows.net',
DATABASE_NAME='ShardMgtDB',
CREDENTIAL = cred,
SHARD_MAP_NAME='ElasticScaleWithEF'
);

-- The Shard Map Name above can be found in
-- SELECT * FROM __ShardManagement.ShardMapsGlobal

select * from sys.external_data_sources;




-- DROP EXTERNAL TABLE [dbo].[Blogs]
-- Note that the external table name must match the name of table in the shards
-- Note we use ROUND_ROBIN to send query to all shards as we 
CREATE EXTERNAL TABLE [dbo].[Blogs](
	[BlogId] [int]  NOT NULL,
	[Name] [nvarchar](max) NULL,
	[Url] [nvarchar](max) NULL
)
WITH
(
DATA_SOURCE = AllBlogsDBs,
DISTRIBUTION=ROUND_ROBIN
);

select * from sys.external_tables;

-- RUN Queries (Note more complex query like orderBy BlogId times out)
select * from  [dbo].[Blogs]
select * from  [dbo].[Blogs] order by BlogId
