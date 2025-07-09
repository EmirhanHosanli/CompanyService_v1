FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src


COPY ["CompanyService.csproj", "./"]
RUN dotnet restore "CompanyService.csproj"


COPY . .
RUN dotnet build "CompanyService.csproj" -c Release -o /app/build


RUN dotnet publish "CompanyService.csproj" -c Release -o /app/publish


FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CompanyService.dll"]
