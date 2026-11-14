FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["GestaoFinanceira.Api.csproj", "."]
RUN dotnet restore "GestaoFinanceira.Api.csproj"
COPY . .
RUN dotnet publish "GestaoFinanceira.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

# Números e mensagens no formato brasileiro (R$ 1.234,56)
ENV LANG=pt_BR.UTF-8

COPY --from=build /app/publish .

# Pasta dos comprovantes, gravável pelo usuário "app", que roda sem privilégios
RUN mkdir -p Armazenamento/Comprovantes && chown -R app:app Armazenamento

USER app
ENTRYPOINT ["dotnet", "GestaoFinanceira.Api.dll"]