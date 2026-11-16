FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./

COPY src/FinFlow.Db.Migrator/FinFlow.Db.Migrator.csproj src/FinFlow.Db.Migrator/

RUN dotnet restore src/FinFlow.Db.Migrator/FinFlow.Db.Migrator.csproj

COPY src/ src/

RUN dotnet publish src/FinFlow.Db.Migrator/FinFlow.Db.Migrator.csproj -c Release -o /app/publish --no-restore -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "FinFlow.Db.Migrator.dll"]
