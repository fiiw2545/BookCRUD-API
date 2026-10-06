# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY BookAPI/BookAPI.csproj BookAPI/
RUN dotnet restore BookAPI/BookAPI.csproj

COPY BookAPI/ BookAPI/

RUN dotnet publish BookAPI/BookAPI.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:${PORT}

ENTRYPOINT ["dotnet", "BookAPI.dll"]
