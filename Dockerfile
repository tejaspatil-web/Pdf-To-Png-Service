# build
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# copy csproj and restore first to leverage cache
COPY Pdf-To-Png/*.csproj ./Pdf-To-Png/
WORKDIR /src/Pdf-To-Png
RUN dotnet restore

# copy everything and publish
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Pdf-To-Png.dll"]