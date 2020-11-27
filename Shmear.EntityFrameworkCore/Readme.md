# This doesn't seem to be working anymore - bmiller 11/26/2020
## Help command for Entity Framework Core
##get-help entityframeworkcore

## Used to build the model directory
##Scaffold-DbContext "Server=localhost;Database=Card.Dev;Trusted_Connection=True;" Microsoft.EntityFrameworkCore.SqlServer -OutputDir EntityFrameworkCore\SqlServer\Models -context CardContext -f -v 

## Changes made after building the model directory
##   Comment out default connection string if CardContext isn't configured

# This worked - bmiller - 11/26/2020
dotnet tool install dotnet-ef --global
dotnet-ef migrations add card
dotnet ef database update