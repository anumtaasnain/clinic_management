# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.sln .
COPY Clinic_Management/*.csproj ./Clinic_Management/
RUN dotnet restore

# Copy everything else and build
COPY . .
WORKDIR /app/Clinic_Management
RUN dotnet publish -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/Clinic_Management/out ./
ENTRYPOINT ["dotnet", "Clinic_Management.dll"]
