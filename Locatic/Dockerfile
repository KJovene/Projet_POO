# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore (couche mise en cache tant que le .csproj ne change pas)
COPY Locatic.csproj ./
RUN dotnet restore Locatic.csproj

# Publish
COPY . ./
RUN dotnet publish Locatic.csproj -c Release -o /app/publish /p:UseAppHost=false

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Locatic.dll"]
