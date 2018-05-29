# Help command for Entity Framework Core
get-help entityframeworkcore

# Used to build the model directory
Scaffold-DbContext "Server=localhost;Database=Card.Dev;Trusted_Connection=True;" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models

# Changes made after building the model directory
#rename Card_DevContext.cs to CardContext.cs