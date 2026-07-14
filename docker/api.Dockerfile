FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY backend/src/ src/
RUN dotnet restore src/Innovayse.Email.API/Innovayse.Email.API.csproj
RUN dotnet publish src/Innovayse.Email.API/Innovayse.Email.API.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Innovayse.Email.API.dll"]
