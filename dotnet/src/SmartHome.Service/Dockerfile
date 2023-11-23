FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/SmartHome.Service/SmartHome.Service.csproj", "src/SmartHome.Service/"]
COPY ["src/SmartHome.Infrastructure.CatScale/SmartHome.Infrastructure.CatScale.csproj", "src/SmartHome.Infrastructure.CatScale/"]
COPY ["src/SmartHome.Domain/SmartHome.Domain.csproj", "src/SmartHome.Domain/"]
COPY ["src/SmartHome.Infrastructure/SmartHome.Infrastructure.csproj", "src/SmartHome.Infrastructure/"]
COPY ["src/SmartHome.Utils/SmartHome.Utils.csproj", "src/SmartHome.Utils/"]
COPY ["src/SmartHome.Infrastructure.Zigbee2Mqtt/SmartHome.Infrastructure.Zigbee2Mqtt.csproj", "src/SmartHome.Infrastructure.Zigbee2Mqtt/"]
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
