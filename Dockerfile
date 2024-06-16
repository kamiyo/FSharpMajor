FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine as build

RUN apk update && apk upgrade

WORKDIR /app

COPY . .

WORKDIR ./src/FSharpMajor
RUN dotnet publish -c Release -o build

FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine
WORKDIR /app
COPY --from=build /src/src/FSharpMajor/build .
COPY --from=build /src/src/FSharpMajor/DB/migrations ./DB/migrations
EXPOSE 8080
ENTRYPOINT [ "dotnet", "FSharpMajor.dll" ]