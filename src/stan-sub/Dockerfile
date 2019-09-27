FROM mcr.microsoft.com/dotnet/core/aspnet:3.0.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443


FROM dotnetcore-nats-play/base-build AS build
WORKDIR /src
RUN dotnet publish stan-sub -c Release -o out --no-restore --no-build

FROM base AS final
WORKDIR /app
COPY --from=build /src/out .
ENTRYPOINT ["dotnet", "stan-sub.dll"]