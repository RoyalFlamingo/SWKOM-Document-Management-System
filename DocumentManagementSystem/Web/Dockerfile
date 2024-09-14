FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Web/Web.csproj", "Web/"]
COPY ["Business/Business.csproj", "Business/"]
COPY ["Persistence/Persistence.csproj", "Persistence/"]
RUN dotnet restore "Web/Web.csproj"
COPY . .
WORKDIR "/src/Web"
RUN dotnet build "Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS="https://+:8081;http://+:8080"
ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "Web.dll"]
