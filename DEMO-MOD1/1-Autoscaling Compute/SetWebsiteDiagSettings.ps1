
Set-ExecutionPolicy RemoteSigned

# Interactive login to your subscription
Add-AzureAccount

# List all subscriptions 
Get-AzureSubscription

# Select subscription from a list by name 
Select-AzureSubscription -SubscriptionName AzureMSDN

# list all websites
Get-AzureWebsite

# Get websites matching pattern
Get-AzureWebsite | 
    Where-Object { $_.Name -like "*p-seller*" } | 
    ForEach-Object { 
    Write-Host $_.Name
}


# Name of the website we want to configure
$webSiteName ="p-seller-tracing"

# create and push settings to your site
$settings = @{  "CloudShopName" = "Contoso Cloud Shop"}

# these are the connection strings that we will configure

# SQl Azure DB
$database="gpses.database.windows.net"
$dbuser = "micham@gpses"
$options = "Trusted_Connection=False;Encrypt=True;"


$connectionStrings = (`
@{Name = "AdventureWorksEntities"; Type = "Custom"; ConnectionString = 'metadata=res://*/Models.AdventureWorks.csdl|res://*/Models.AdventureWorks.ssdl|res://*/Models.AdventureWorks.msl;provider=System.Data.SqlClient;provider connection string="data source='+$database+',1433;Database=AdventureWorks2012;User ID='+$dbuser+';Password=RedW1ne!;'+$options+'Connection Timeout=30;multipleactiveresultsets=True;App=EntityFramework"' },`
@{Name = "DefaultConnection"; Type = "SQLAzure"; ConnectionString = 'Data Source='+$database+',1433;Database=AdventureWorks2012;User ID='+$dbuser+';Password=RedW1ne!;'+$options+'Connection Timeout=30;MultipleActiveResultSets=True'} )  


#set application settings and connection strings
Set-AzureWebsite $webSiteName -AppSettings $settings -ConnectionStrings $connectionStrings


$webSiteConfiguration = Get-AzureWebsite -Name $webSiteName
$webSiteConfiguration.ConnectionStrings
$webSiteConfiguration.AppSettings

#diagnostic config
$webSiteConfiguration.RemoteDebuggingEnabled = "True"
$webSiteConfiguration.RemoteDebuggingVersion = "VS2013"

# site diagniostics
$webSiteConfiguration.DetailedErrorLoggingEnabled = "True"
$webSiteConfiguration.HttpLoggingEnabled = "True"
$webSiteConfiguration.RequestTracingEnabled = "True"

# application level tracing currently does not seem to be functional
$webSiteConfiguration.AzureDriveTraceEnabled = "True"
$webSiteConfiguration.AzureDriveTraceLevel = "Verbose"
$webSiteConfiguration.AzureBlobTraceLevel = "Error"
$webSiteConfiguration.AzureBlobTraceEnabled = "True"
$webSiteConfiguration.AzureTableTraceLevel = "Error"
$webSiteConfiguration.AzureTableTraceEnabled = "True"


# set and get the configuration for website
Set-AzureWebsite $webSiteName -SiteWithConfig $webSiteConfiguration
$webSiteConfiguration = Get-AzureWebsite -Name $webSiteName


# shows website in browser
Show-AzureWebsite -Name $webSiteName


