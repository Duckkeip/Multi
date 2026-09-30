# 1. Build stage với .NET 10 SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy file .csproj và restore
COPY ["ChatServer/ChatServer.csproj", "ChatServer/"]
COPY ["ChatProtocol/ChatProtocol.csproj", "ChatProtocol/"]
RUN dotnet restore "ChatServer/ChatServer.csproj"

# Copy toàn bộ code và publish
COPY . .
WORKDIR "/src/ChatServer"
RUN dotnet publish "ChatServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Runtime stage nhẹ (Alpine)
FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .

# Mở cổng 5050 cho ChatServer
EXPOSE 5050
ENV PORT=5050

ENTRYPOINT ["dotnet", "ChatServer.dll"]
