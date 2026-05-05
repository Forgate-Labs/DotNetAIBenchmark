FROM mcr.microsoft.com/dotnet/sdk:10.0

ARG PI_NPM_PACKAGE=@mariozechner/pi-coding-agent

ENV DOTNET_NOLOGO=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    PI_SKIP_VERSION_CHECK=1 \
    PI_TELEMETRY=0

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl git gnupg bash \
    && curl -fsSL https://deb.nodesource.com/setup_24.x | bash - \
    && apt-get install -y --no-install-recommends nodejs \
    && npm install -g ${PI_NPM_PACKAGE} \
    && npm cache clean --force \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /workspace
