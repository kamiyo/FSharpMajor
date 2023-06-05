FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine as build

WORKDIR /src

COPY . .

WORKDIR ./src/FSharpMajor
RUN dotnet publish -c Release -o build

FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine
WORKDIR /src
COPY --from=build /src/src/FSharpMajor/build .
EXPOSE 8080
ENTRYPOINT [ "dotnet", "FSharpMajor.dll" ]