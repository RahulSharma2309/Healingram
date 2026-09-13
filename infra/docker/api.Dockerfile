FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/Directory.Build.props backend/Directory.Build.props
COPY backend/src backend/src
COPY backend/db backend/db
WORKDIR /src/backend/src/Healingram.Api
RUN dotnet publish Healingram.Api.csproj -c Release -o /app/publish --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
COPY backend/db /app/db
ENV ASPNETCORE_URLS=http://+:5080
EXPOSE 5080
ENTRYPOINT ["dotnet", "Healingram.Api.dll"]
