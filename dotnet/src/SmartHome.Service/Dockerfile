FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/SmartHome.Domain/SmartHome.Domain.csproj",                                         "src/SmartHome.Domain/"]
COPY ["src/SmartHome.Infrastructure/SmartHome.Infrastructure.csproj",                         "src/SmartHome.Infrastructure/"]
COPY ["src/SmartHome.Infrastructure.CatScale/SmartHome.Infrastructure.CatScale.csproj",       "src/SmartHome.Infrastructure.CatScale/"]
COPY ["src/SmartHome.Infrastructure.Influx/SmartHome.Infrastructure.Influx.csproj",           "src/SmartHome.Infrastructure.Influx/"]
COPY ["src/SmartHome.Infrastructure.Mqtt/SmartHome.Infrastructure.Mqtt.csproj",               "src/SmartHome.Infrastructure.Mqtt/"]
COPY ["src/SmartHome.Infrastructure.Zigbee2Mqtt/SmartHome.Infrastructure.Zigbee2Mqtt.csproj", "src/SmartHome.Infrastructure.Zigbee2Mqtt/"]
COPY ["src/SmartHome.Service/SmartHome.Service.csproj",                                       "src/SmartHome.Service/"]
COPY ["src/SmartHome.Utils/SmartHome.Utils.csproj",                                           "src/SmartHome.Utils/"]
RUN dotnet restore "src/SmartHome.Service/SmartHome.Service.csproj"
COPY . .
WORKDIR "/src/src/SmartHome.Service"
RUN dotnet build "SmartHome.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SmartHome.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartHome.Service.dll"]
   