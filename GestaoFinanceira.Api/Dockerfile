FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["GestaoFinanceira.Api.csproj", "."]
RUN dotnet restore "./GestaoFinanceira.Api.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "GestaoFinanceira.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GestaoFinanceira.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GestaoFinanceira.Api.dll"]