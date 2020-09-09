FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster as build
WORKDIR /src
COPY token.txt .
COPY SharedVolume SharedVolume
COPY TU20Bot TU20Bot

FROM build as publish
WORKDIR /src/TU20Bot
RUN dotnet publish -c Release -o /src/publish

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim as final
WORKDIR /app
COPY --from=publish /src/publish .
ENTRYPOINT ["dotnet", "TU20Bot.dll"]