#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

# FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
# WORKDIR /app


# FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
# WORKDIR /src
# COPY ["mvcapp/mvcapp.csproj", "mvcapp/"]
# RUN dotnet restore "mvcapp/mvcapp.csproj"
# COPY . .
# WORKDIR "/src/mvcapp"
# RUN dotnet build "mvcapp.csproj" -c Release -o /app/build

# FROM build AS publish
# RUN dotnet publish "mvcapp.csproj" -c Release -o /app/publish

# FROM mcr.microsoft.com/dotnet/aspnet:5.0 
# WORKDIR /app
# COPY --from=publish /app/publish .
# ENTRYPOINT ["dotnet", "mvcapp.dll"]



FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY *.sln .
COPY mvcapp/*.csproj ./mvcapp/
RUN dotnet restore

# copy everything else and build app
COPY mvcapp/. ./mvcapp/
WORKDIR /source/mvcapp
RUN dotnet publish "mvcapp.csproj" -c Release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "mvcapp.dll"]