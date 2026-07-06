FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["SaszetApp.Api.csproj", "./"]
RUN dotnet restore "SaszetApp.Api.csproj"
COPY . .
RUN dotnet build "SaszetApp.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SaszetApp.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SaszetApp.Api.dll"]
