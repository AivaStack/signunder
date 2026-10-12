# Build context is the repository root: docker build -f src/Signunder.Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY global.json Directory.Build.props ./
COPY proto/ proto/
COPY src/Signunder.Core/ src/Signunder.Core/
COPY src/Signunder.Rules/ src/Signunder.Rules/
COPY src/Signunder.Api/ src/Signunder.Api/
RUN dotnet publish src/Signunder.Api -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Signunder.Api.dll"]
