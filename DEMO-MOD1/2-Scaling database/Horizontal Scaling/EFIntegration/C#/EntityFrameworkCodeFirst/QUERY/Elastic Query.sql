-- DROP EXTERNAL DATA SOURCE AllBlogsDBs
CREATE EXTERNAL DATA SOURCE AllBlogsDBs
WITH
(
TYPE=SHARD_MAP_MANAGER,
LOCATION='gpsesV12.database.windows.net',
DATABASE_NAME='ShardMgtDB',
CREDENTIAL = morcinim,
SHARD_MAP_NAME='ElasticScaleWithEF'
);
select * from sys.external_data_sources;
-- The Shard Map Name
-- SELECT * FROM __ShardManagement.ShardMapsGlobal

-- DROP EXTERNAL TABLE [dbo]
CREATE EXTERNAL TABLE [dbo].[AllBlogs](
	[BlogId] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NULL,
	[Url] [nvarchar](max) NULL
)
WITH
(
DATA_SOURCE = AllBlogsDBs,
DISTRIBUTION=ROUND_ROBIN
);

select * from sys.external_tables;

-- RUN Queries
select Name from [AllBlogs] orderby Url
