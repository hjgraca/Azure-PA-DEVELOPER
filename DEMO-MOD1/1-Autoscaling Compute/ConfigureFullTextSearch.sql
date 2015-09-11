 -- http://azure.microsoft.com/blog/2015/04/30/full-text-search-is-now-available-for-preview-in-azure-sql-database/
CREATE FULLTEXT CATALOG ftCatalog AS DEFAULT;

--for AdventureWorksLite SQL Azure database 
CREATE UNIQUE INDEX ui_ukProductDescription ON SalesLT.ProductDescription(ProductDescriptionID); 
CREATE FULLTEXT INDEX ON SalesLT.ProductDescription(Description) KEY INDEX ui_ukProductDescription ON ftCatalog; 
ALTER FULLTEXT INDEX ON SalesLT.ProductDescription ENABLE;
GO 
ALTER FULLTEXT INDEX ON SalesLT.ProductDescription START FULL POPULATION;
GO
CREATE FULLTEXT INDEX ON SalesLT.[Product] KEY INDEX [PK_Product_ProductID] ON ([ftCatalog]) WITH (CHANGE_TRACKING AUTO)
GO
ALTER FULLTEXT INDEX ON SalesLT.[Product] ADD ([Name])
GO
ALTER FULLTEXT INDEX ON SalesLT.[Product] ENABLE
GO 
ALTER FULLTEXT INDEX ON SalesLT.[Product] START FULL POPULATION;
--Test full text search for Products
Select Name from SalesLT.Product Where Freetext(*,'Bike')
