FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY ["SGE.API/SGE.API.csproj", "SGE.API/"]
COPY ["SGE.Application/SGE.Application.csproj", "SGE.Application/"]
COPY ["SGE.Infrastructure/SGE.Infrastructure.csproj", "SGE.Infrastructure/"]
COPY ["SGE.Core/SGE.Core.csproj", "SGE.Core/"]

RUN dotnet restore "SGE.API/SGE.API.csproj"

COPY . .
WORKDIR "/app/SGE.API"
RUN dotnet build "SGE.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SGE.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SGE.API.dll"]
