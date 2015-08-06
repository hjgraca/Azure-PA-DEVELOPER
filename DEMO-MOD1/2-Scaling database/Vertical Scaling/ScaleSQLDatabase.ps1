
# http://azure.microsoft.com/blog/2013/02/07/windows-azure-sql-database-management-with-powershell/
function Scale-Database {
    [CmdletBinding()]
    param (
        [string]$sqlserverName = 'gps',    
        [string]$databaseName ='AdventureWorks',
        [string]$edition ='Standard',
        [string]$serviceLevel = 'S0' )

   $db=Get-AzureSqlDatabase  -ServerName $sqlserverName -DatabaseName $databaseName
   Write-Host $db.Name "current performance level is" $db.ServiceObjective.Name

   $so=Get-AzureSqlDatabaseServiceObjective -ServerName $sqlserverName -ServiceObjectiveName $serviceLevel 

   Write-Host "setting edition" $edition "and performance level" $so.Name "for database " $db.Name
   Set-AzureSqlDatabase -ServerName $sqlserverName -DatabaseName $db.Name -Edition $edition -ServiceObjective $so -Force
  
    $newdb=Get-AzureSqlDatabase -ServerName $sqlserverName -DatabaseName $databaseName
    Write-Host $newdb.Name   "scale request to performance level"  $newdb.ServiceObjectiveName "is" $newdb.ServiceObjectiveAssignmentStateDescription
}

# Login to subscription using Azure Active Directory credentials 
Add-AzureAccount
# List subsctiptions
Get-AzureSubscription
$subscriptionName = "AzureMSDN"
$subscriptionName = "DYNSMSPAzureGPS-micham"

#set desired subscription to work with
Select-AzureSubscription -SubscriptionName $subscriptionName

#List servers available
Get-AzureSqlDatabaseServer

$sqlserverName ='gps'
$databaseName = 'AdventureWorks'

#scale up
$serviceLevel = 'S0'
$edition = 'Standard'

#scale down
$serviceLevel = 'Basic'
$edition = 'Basic'


Scale-Database -sqlserverName $sqlserverName -databaseName $databaseName -edition $edition -serviceLevel $serviceLevel
# setting serivice level objective will take 15 -30 seconds..if scaling down

 $newdb=Get-AzureSqlDatabase -ServerName $sqlserverName -DatabaseName $databaseName
 Write-Host $newdb.Name   "scale request to performance level"  $newdb.ServiceObjectiveName "is" $newdb.ServiceObjectiveAssignmentStateDescription
