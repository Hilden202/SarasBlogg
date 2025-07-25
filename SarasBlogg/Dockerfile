# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SarasBlogg.csproj", "./"]
RUN dotnet restore "SarasBlogg.csproj"
COPY . .
RUN dotnet publish "SarasBlogg.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SarasBlogg.dll"]