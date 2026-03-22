FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Hammer.User.slnx ./
COPY src/Hammer.User.Domain/Hammer.User.Domain.csproj src/Hammer.User.Domain/
COPY src/Hammer.User.Application/Hammer.User.Application.csproj src/Hammer.User.Application/
COPY src/Hammer.User.Infrastructure/Hammer.User.Infrastructure.csproj src/Hammer.User.Infrastructure/
COPY src/Hammer.User.Api/Hammer.User.Api.csproj src/Hammer.User.Api/
RUN dotnet restore src/Hammer.User.Api/Hammer.User.Api.csproj

COPY src/ src/
RUN dotnet publish src/Hammer.User.Api/Hammer.User.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN groupadd --system --gid 1001 appgroup && \
    useradd --system --uid 1001 --gid appgroup --no-create-home appuser

COPY --from=build /app .

USER appuser
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Hammer.User.Api.dll"]
