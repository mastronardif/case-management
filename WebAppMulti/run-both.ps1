# Run App1
Start-Process "dotnet" "run --launch-profile App1"
Start-Sleep -Seconds 5
Start-Process "http://localhost:5173/swagger"

# Run App2
Start-Process "dotnet" "run --launch-profile App2"
Start-Sleep -Seconds 5
Start-Process "http://localhost:5174/swagger"



cd C:\nginx
.\nginx.exe -c .\conf\nginx.conf
