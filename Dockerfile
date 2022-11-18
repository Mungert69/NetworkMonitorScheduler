
# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:7.0-jammy-amd64
WORKDIR /app
COPY bin/release/netcoreapp7.0/publish/  /app 
ENTRYPOINT ["dotnet","./NetworkMonitorScheduler.dll"]

