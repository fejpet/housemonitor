FROM mcr.microsoft.com/devcontainers/dotnet:1-8.0-bookworm

# Install gcc (for BuildLinuxX11Stubs MSBuild target) and Avalonia X11/OpenGL runtime dependencies (REQ-020, REQ-036)
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends \
        build-essential \
        gcc \
        libx11-6 \
        libx11-dev \
        libice6 \
        libsm6 \
        libxcursor1 \
        libxext6 \
        libxi6 \
        libxrandr2 \
        libfontconfig1 \
        libfreetype6 \
        libgl1 \
        x11-xserver-utils \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*
