FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Components/CORS/CORS.Backend/CORS.Backend.csproj", "Components/CORS/CORS.Backend/"]
RUN dotnet restore "Components/CORS/CORS.Backend/CORS.Backend.csproj"
COPY . .
WORKDIR "/src/Components/CORS/CORS.Backend"
RUN dotnet build "CORS.Backend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CORS.Backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CORS.Backend.dll"]
