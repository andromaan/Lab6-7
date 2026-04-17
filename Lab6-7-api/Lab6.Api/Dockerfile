FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Lab6.Api/Lab6.Api.csproj", "Lab6.Api/"]
RUN dotnet restore "Lab6.Api/Lab6.Api.csproj"
COPY . .
WORKDIR "/src/Lab6.Api"
RUN dotnet build "Lab6.Api.csproj" -c Release -o /app/build
RUN dotnet publish "Lab6.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Lab6.Api.dll"]
