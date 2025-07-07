# 1. ASP.NET Core runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# 2. .NET SDK (build stage)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Proje dosyalarýný kopyala ve restore et
COPY ["CompanyService.csproj", "./"]
RUN dotnet restore "CompanyService.csproj"

# Tüm kaynak kodlarý kopyala ve build et
COPY . .
RUN dotnet build "CompanyService.csproj" -c Release -o /app/build

# Yayýnlama iþlemi
RUN dotnet publish "CompanyService.csproj" -c Release -o /app/publish

# 3. Runtime image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CompanyService.dll"]
