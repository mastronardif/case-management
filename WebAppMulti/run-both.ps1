# Run App1
Start-Process "dotnet" "run --launch-profile App1"
Start-Sleep -Seconds 5
Start-Process "https://localhost:7009/swagger"

# Run App2
Start-Process "dotnet" "run --launch-profile App2"
Start-Sleep -Seconds 5
Start-Process "https://localhost:7010/swagger"



cd C:\nginx
.\nginx.exe -c .\conf\nginx.conf
