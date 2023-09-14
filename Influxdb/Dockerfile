FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Influxdb/Influxdb.csproj", "Influxdb/"]
RUN dotnet restore "Influxdb/Influxdb.csproj"
COPY . .
WORKDIR "/src/Influxdb"
RUN dotnet build "Influxdb.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Influxdb.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Influxdb.dll"]
