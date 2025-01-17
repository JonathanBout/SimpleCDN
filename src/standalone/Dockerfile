# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.
# These ARGs allow for swapping out the base used to make the final image when debugging from VS
ARG LAUNCHING_FROM_VS
# This sets the base image for final, but only if LAUNCHING_FROM_VS has been defined
ARG FINAL_BASE_IMAGE=${LAUNCHING_FROM_VS:+aotdebug}

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
ENV CDN__DataRoot=/data
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# This stage is used as the base for the final stage when launching from VS to support debugging in regular mode (Default when not using the Debug configuration)
FROM base AS aotdebug
USER root
# Install GDB to support native debugging
RUN apt-get update -y \
    && apt-get install -y --no-install-recommends \
    gdb \
    && rm -rf /var/lib/apt/lists/*
USER app

# This stage builds the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG DOTNET_BUILD_PLATFORM=linux-x64
WORKDIR /code

# Install clang/zlib1g-dev dependencies for publishing to native
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    clang zlib1g-dev \
    && rm -rf /var/lib/apt/lists/*

# as csproj files don't always change, we can copy them first and potentially cache the restore step
COPY ["./src/standalone/SimpleCDN.Standalone.csproj", "src/standalone/"]
COPY ["./src/core/SimpleCDN.csproj", "src/core/"]
COPY ["./extensions/Redis/SimpleCDN.Extensions.Redis.csproj", "extensions/Redis/"]

RUN dotnet restore "/code/src/standalone/SimpleCDN.Standalone.csproj" -r $DOTNET_BUILD_PLATFORM --nologo -v:m

# now copy the rest of the files and build. This part is unlikely to not change,
# as with most/all releases the code changes
COPY . .
WORKDIR "/code/src/standalone"
RUN dotnet build "./SimpleCDN.Standalone.csproj" --no-restore -c $BUILD_CONFIGURATION \
    -r $DOTNET_BUILD_PLATFORM --nologo -v:m

# now use the build output to publish the app into its final form
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG DOTNET_BUILD_PLATFORM=linux-x64
RUN dotnet publish "./SimpleCDN.Standalone.csproj" --no-build -c $BUILD_CONFIGURATION \
    -r $DOTNET_BUILD_PLATFORM -o /app/publish -p:UseAppHost=true --nologo -v:m

# This stage copies just the published output into a new image
FROM ${FINAL_BASE_IMAGE:-mcr.microsoft.com/dotnet/runtime-deps:9.0} AS final
WORKDIR /app

ENV CDN__DataRoot=/data
ENV ASPNETCORE_URLS=http://+:8080

# install curl for healthchecks
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    curl \
    && rm -rf /var/lib/apt/lists/*
USER app

COPY --from=publish /app/publish .

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 CMD curl --silent --fail http://localhost:8080/_cdn/server/health || exit 1

EXPOSE 8080

ENTRYPOINT ["./SimpleCDN.Standalone"]
