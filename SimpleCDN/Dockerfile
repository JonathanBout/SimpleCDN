# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# These ARGs allow for swapping out the base used to make the final image when debugging from VS
ARG LAUNCHING_FROM_VS
# This sets the base image for final, but only if LAUNCHING_FROM_VS has been defined
ARG FINAL_BASE_IMAGE=${LAUNCHING_FROM_VS:+aotdebug}

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
ENV CDN_DATA_ROOT=/data
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
# Install clang/zlib1g-dev dependencies for publishing to native
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    clang zlib1g-dev
ARG BUILD_CONFIGURATION=Release
ARG DOTNET_BUILD_PLATFORM=linux-x64
WORKDIR /src
COPY ["SimpleCDN/SimpleCDN.csproj", "SimpleCDN/"]
RUN dotnet restore "./SimpleCDN/SimpleCDN.csproj"
COPY . .
WORKDIR "/src/SimpleCDN"
RUN dotnet build "./SimpleCDN.csproj" -c $BUILD_CONFIGURATION -r $DOTNET_BUILD_PLATFORM -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG DOTNET_BUILD_PLATFORM=linux-x64
RUN dotnet publish "./SimpleCDN.csproj" -c $BUILD_CONFIGURATION  -r $DOTNET_BUILD_PLATFORM -o /app/publish /p:UseAppHost=true

# This stage is used as the base for the final stage when launching from VS to support debugging in regular mode (Default when not using the Debug configuration)
FROM base AS aotdebug
USER root
# Install GDB to support native debugging
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    gdb
USER app

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM ${FINAL_BASE_IMAGE:-mcr.microsoft.com/dotnet/runtime-deps:9.0} AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Install curl for use in healthchecks. Due to possible changes
# to ports, we can't specify the HEALTHCHECK in the Dockerfile
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    curl

ENV CDN_DATA_ROOT=/data
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["./SimpleCDN"]