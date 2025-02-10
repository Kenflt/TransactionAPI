# Use official .NET SDK as build environment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy everything into the container
COPY . ./

# Restore dependencies
RUN dotnet restore

# Build & publish the app
RUN dotnet publish -c Release -o out

# Use lightweight runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

# Expose API port
EXPOSE 8080

# Start the application
ENTRYPOINT ["dotnet", "TransactionAPI.dll"]
