FROM mcr.microsoft.com/dotnet/core/runtime:3.1

COPY /TipBot/bin/Release/netcoreapp3.1/publish/ app/

ENTRYPOINT dotnet app/TipBot.dll "$MONGO_URL" "$ENVIRONMENT" "$TOKEN" "$PREFIX" "$PK" "$PK2" "$RPC"
