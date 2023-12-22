FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["WebSecurityMechanisms.App/WebSecurityMechanisms.App.csproj", "WebSecurityMechanisms.App/"]
RUN dotnet restore "WebSecurityMechanisms.App/WebSecurityMechanisms.App.csproj"
COPY . .
WORKDIR "/src/WebSecurityMechanisms.App"
RUN dotnet build "WebSecurityMechanisms.App.csproj" -c Release -o /app/build
RUN dotnet publish "WebSecurityMechanisms.App.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM nginx:1.25.3-alpine3.18 AS final
WORKDIR /user/share/nginx/html
COPY --from=build /app/publish/wwwroot .
COPY WebSecurityMechanisms.App/nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
