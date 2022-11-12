
# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:6.0-jammy-amd64
WORKDIR /app
COPY bin/release/netcoreapp6.0/publish/  /app 
COPY monitor.csv ./
ENTRYPOINT ["dotnet","./NetworkMonitor.dll"]

