﻿FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["WebSecurityMechanisms.Api/WebSecurityMechanisms.Api.csproj", "WebSecurityMechanisms.Api/"]
RUN dotnet restore "WebSecurityMechanisms.Api/WebSecurityMechanisms.Api.csproj"
COPY . .
WORKDIR "/src/WebSecurityMechanisms.Api"
RUN dotnet build "WebSecurityMechanisms.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebSecurityMechanisms.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebSecurityMechanisms.Api.dll"]
