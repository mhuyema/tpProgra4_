# Dockerfile único para los 3 microservicios: se elige cuál construir con --build-arg PROJECT.
# Contexto de build = raíz del repo (los proyectos referencian a Broker.Messaging y Payment.Contracts).
#   docker build --build-arg PROJECT=src/Booking/Booking.Api --build-arg ASSEMBLY=Booking.Api -t booking .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT
WORKDIR /repo
COPY src/ src/
# Cache de NuGet compartido entre builds: los paquetes no se re-descargan al cambiar código.
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "$PROJECT" -c Release -o /app --nologo

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# Puerto interno único; el compose decide qué puerto del host se publica.
ENV ASPNETCORE_URLS=http://+:8080
# Usuario no root (la imagen aspnet trae el usuario "app").
USER app
EXPOSE 8080
COPY --from=build /app .
# Nombre del ensamblado a ejecutar (Booking.Api, Payment.Api, ...), lo pasa el compose.
ARG ASSEMBLY
ENV ASSEMBLY=$ASSEMBLY
ENTRYPOINT ["sh", "-c", "exec dotnet /app/${ASSEMBLY}.dll"]
