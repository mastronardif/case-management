# Built by build_and_ship.py from a staged copy of WebAppMulti that holds only the
# folders the API needs (no 42A, Database/Scripts, bin/obj, or local data files).
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY WebAppMulti.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
RUN mkdir -p /app/logs && chown app:app /app/logs
USER app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "WebAppMulti.dll"]
