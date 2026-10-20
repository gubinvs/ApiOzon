# Base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5148
ENV ASPNETCORE_URLS=http://+:5148

# SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ApiOzon.csproj", "./"]
RUN dotnet restore "ApiOzon.csproj"
COPY . .
RUN dotnet build "ApiOzon.csproj" -c Release -o /app/build
RUN dotnet publish "ApiOzon.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Создаем папку для ключей и даем права
USER root
RUN mkdir -p /app/dpkeys && chown -R app:app /app/dpkeys
USER app

ENTRYPOINT ["dotnet", "ApiOzon.dll"]