FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["WebSecurityMechanisms.Proxy/WebSecurityMechanisms.Proxy.csproj", "WebSecurityMechanisms.Proxy/"]
RUN dotnet restore "WebSecurityMechanisms.Proxy/WebSecurityMechanisms.Proxy.csproj"
COPY . .
WORKDIR "/src/WebSecurityMechanisms.Proxy"
RUN dotnet build "WebSecurityMechanisms.Proxy.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebSecurityMechanisms.Proxy.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebSecurityMechanisms.Proxy.dll"]
