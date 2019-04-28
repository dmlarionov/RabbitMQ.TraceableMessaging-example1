FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build
WORKDIR /src
COPY . .
RUN dotnet publish ./bar/bar.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/core/runtime:2.2 AS runtime
COPY --from=build /out /app
WORKDIR /app
ENTRYPOINT [ "dotnet", "bar.dll" ]