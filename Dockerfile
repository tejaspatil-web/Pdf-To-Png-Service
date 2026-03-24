# build
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

COPY Pdf-To-Png/Pdf-To-Png.csproj ./Pdf-To-Png/
WORKDIR /src/Pdf-To-Png
RUN dotnet restore

COPY Pdf-To-Png/. .
RUN dotnet publish -c Release -o /app/publish --no-restore

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app

RUN apt-get update && \
    apt-get install -y poppler-utils && \
    rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Pdf-To-Png.dll"]