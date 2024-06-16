# F♯major

FSharpMajor is a [SubsonicAPI](https://www.subsonic.org/pages/api.jsp) compatible music library built in [F#](https://fsharp.org/) and dotnet

It is currently a work in progress.

## Developing
- Install [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0), [postgresql](https://www.postgresql.org/), [dbmate](https://github.com/amacneil/dbmate)
- Clone repo
- Populate `.env` file (see `.env.sample`)
- Navigate to `./src/FSharpMajor`
- Run `dbmate up`
- Run `dotnet run`
- API is accessible at `[host]:[port]/rest/[endpoint]`