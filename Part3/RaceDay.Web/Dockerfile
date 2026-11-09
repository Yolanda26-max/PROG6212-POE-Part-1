FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY RaceDay.Web.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ApiSettings__BaseUrl=http://host.docker.internal:5065/
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]