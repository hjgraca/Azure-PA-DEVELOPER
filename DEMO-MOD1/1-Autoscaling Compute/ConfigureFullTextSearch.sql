 --create Full Text Catalogue
 -- http://azure.microsoft.com/blog/2015/04/30/full-text-search-is-now-available-for-preview-in-azure-sql-database/
CREATE FULLTEXT CATALOG ftCatalog AS DEFAULT;

--for local AdventureWorksLite SQL Azure database
CREATE UNIQUE INDEX ui_ukProductDescription ON SalesLT.ProductDescription(ProductDescriptionID); 
CREATE FULLTEXT INDEX ON SalesLT.ProductDescription(Description) KEY INDEX ui_ukProductDescription ON ftCatalog; 

-- local AdventureWorks2012
CREATE UNIQUE INDEX ui_ukProductDescription ON [Production].ProductDescription(ProductDescriptionID); 
CREATE FULLTEXT INDEX ON [Production].ProductDescription(Description) KEY INDEX ui_ukProductDescription ON ftCatalog;

ALTER FULLTEXT INDEX ON [Production].ProductDescription ENABLE; 
GO 
ALTER FULLTEXT INDEX ON [Production].ProductDescription START FULL POPULATION;

--enable full text sereach on product Name
USE [AdventureWorks2012]
GO
CREATE FULLTEXT INDEX ON [Production].[Product] KEY INDEX [PK_Product_ProductID] ON ([ftCatalog]) WITH (CHANGE_TRACKING AUTO)
GO
USE [AdventureWorks2012]
GO
ALTER FULLTEXT INDEX ON [Production].[Product] ADD ([Name])
GO
USE [AdventureWorks2012]
GO
ALTER FULLTEXT INDEX ON [Production].[Product] ENABLE
GO
ALTER FULLTEXT INDEX ON [Production].[Product] ENABLE; 
GO 
ALTER FULLTEXT INDEX ON [Production].[Product] START FULL POPULATION;

--Test full text search for Products
Select Name from Production.Product Where Freetext(*,'Bike')
