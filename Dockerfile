
# Imagen base para .NET
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Imagen para compilación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar la solución y restaurar dependencias
COPY ["API/API.csproj", "API/"]
COPY ["Aplication/Aplication.csproj", "Aplication/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Infraestructure/Infraestructure.csproj", "Infraestructure/"]

RUN dotnet restore "API/API.csproj"

# Copiar todo el código y compilar
COPY . .
WORKDIR "/src/API"
RUN dotnet build "API.csproj" -c Release -o /app/build

# Publicar la aplicación
FROM build AS publish
RUN dotnet publish "API.csproj" -c Release -o /app/publish

# Creacion de la Imagen final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "API.dll"]
