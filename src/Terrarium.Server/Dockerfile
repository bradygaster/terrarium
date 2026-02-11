FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["Terrarium.Server/Terrarium.Server.csproj", "Terrarium.Server/"]
COPY ["Terrarium.Services/Terrarium.Services.csproj", "Terrarium.Services/"]
COPY ["Terrarium.Configuration/Terrarium.Configuration.csproj", "Terrarium.Configuration/"]
COPY ["Terrarium.ServiceDefaults/Terrarium.ServiceDefaults.csproj", "Terrarium.ServiceDefaults/"]
COPY ["Directory.Build.props", "./"]
RUN dotnet restore "Terrarium.Server/Terrarium.Server.csproj"
COPY . .
WORKDIR "/src/Terrarium.Server"
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Terrarium.Server.dll"]
