# ---------- Build Stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Planova/Planova.csproj", "Planova/"]
RUN dotnet restore "Planova/Planova.csproj"

COPY . .
WORKDIR "/src/Planova"

RUN dotnet publish "Planova.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ---------- Runtime Stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Create non-root user
RUN adduser --disabled-password --home /app --gecos "" appuser

WORKDIR /app
COPY --from=build /app/publish .

# Change ownership
RUN chown -R appuser /app
USER appuser

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "Planova.dll"]