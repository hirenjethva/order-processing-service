FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/OrderProcessingService.Api/OrderProcessingService.Api.csproj", "OrderProcessingService.Api/"]
COPY ["src/OrderProcessingService.Application/OrderProcessingService.Application.csproj", "OrderProcessingService.Application/"]
COPY ["src/OrderProcessingService.Domain/OrderProcessingService.Domain.csproj", "OrderProcessingService.Domain/"]
COPY ["src/OrderProcessingService.Infrastructure/OrderProcessingService.Infrastructure.csproj", "OrderProcessingService.Infrastructure/"]

RUN dotnet restore "OrderProcessingService.Api/OrderProcessingService.Api.csproj"

COPY src/ .

RUN dotnet publish "OrderProcessingService.Api/OrderProcessingService.Api.csproj" -c Release -o /app/publish --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "OrderProcessingService.Api.dll"]
