# syntax=docker/dockerfile:1
# =============================================================================
# FlowBoard API — imagem multi-stage (ADR-010 a ADR-013)
#   Estágio 1 (build): SDK do .NET compila e publica.
#   Estágio 2 (runtime): somente o runtime ASP.NET + binários publicados,
#                        executando como usuário NÃO-root na porta 8080.
# Imagens base fixadas por digest (atualizadas pelo Dependabot).
# =============================================================================

# ---------- Estágio 1: build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:8c6edfb26c85153f236d26efebd9ad349aec403583f343f201ee67838b23658d AS build

# Versão SemVer e commit, preenchidos pelo pipeline de release.
ARG VERSION=0.0.0-local
WORKDIR /src

# 1) Somente arquivos de dependências: esta camada fica em cache enquanto elas não mudam.
COPY global.json Directory.Build.props Directory.Packages.props nuget.config .editorconfig ./
COPY src/FlowBoard.Api/FlowBoard.Api.csproj src/FlowBoard.Api/packages.lock.json src/FlowBoard.Api/
RUN dotnet restore src/FlowBoard.Api/FlowBoard.Api.csproj --locked-mode

# 2) Código-fonte e publicação (a versão fica gravada no binário e aparece no /health).
COPY src/FlowBoard.Api/ src/FlowBoard.Api/
RUN dotnet publish src/FlowBoard.Api/FlowBoard.Api.csproj -c Release --no-restore \
    -p:Version=${VERSION} -p:UseAppHost=false -o /app/publish

# ---------- Estágio 2: runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:21c3f9798416cba4c0576a947c286231cb71ea2884896cb968253f5c9668f930 AS runtime

ARG VERSION=0.0.0-local
ARG REVISION=unknown
LABEL org.opencontainers.image.title="FlowBoard API" \
      org.opencontainers.image.description="API REST de gestão ágil com Kanban e conceitos do SAFe" \
      org.opencontainers.image.source="https://github.com/renzovaz/flowboard-api" \
      org.opencontainers.image.licenses="MIT" \
      org.opencontainers.image.version="${VERSION}" \
      org.opencontainers.image.revision="${REVISION}"

WORKDIR /app
COPY --from=build --chown=root:root /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_NOLOGO=1

# Usuário não-root "app" (UID 1654), já presente nas imagens oficiais do .NET.
USER $APP_UID
EXPOSE 8080

# Saudável = consegue atender requisições, incluindo o banco (/health/ready).
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD wget -q -O /dev/null http://localhost:8080/health/ready || exit 1

ENTRYPOINT ["dotnet", "FlowBoard.Api.dll"]
