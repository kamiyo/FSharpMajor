namespace FSharpMajor

open System
open System.IO

open Serilog
open Serilog.Sinks.SystemConsole
open Serilog.Context

open FsLibLog
open FsLibLog.Types

open dotenv.net

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

open Giraffe
open Giraffe.SerilogExtensions

open Database

open Router

module Program =
    open System.Xml
    open System.Text
    open System.Xml.Serialization
    open Utils.Environment
    open API.Types
    open System.Threading.Tasks
    open API.System
    open System.Globalization
    open API.User
    open Encryption

    let exitCode = 0


    let configureApp (app: IApplicationBuilder) =
        app.UseGiraffe(SerilogAdapter.Enable rootRouter)

    let writerSettings =
        XmlWriterSettings(Encoding = UTF8Encoding(false), Indent = true, OmitXmlDeclaration = true)

    let (|SomeObj|_|) =
        let ty = typedefof<option<_>>

        fun (a: obj) ->
            let aty = a.GetType()
            let v = aty.GetProperty("Value")

            if aty.IsGenericType && aty.GetGenericTypeDefinition() = ty then
                if a = null then None else Some(v.GetValue(a, [||]))
            else
                None

    let toStringFixBool (v: obj) =
        match v with
        | :? bool -> v.ToString() |> Json.JsonNamingPolicy.CamelCase.ConvertName
        | _ -> v.ToString()

    type CustomXmlSerializer(settings) =
        member __.Write(writer: XmlWriter, node: IXmlElement) =
            writer.WriteStartElement(node.Name, "http://subsonic.org/restapi")

            match node.Attributes with
            | None -> ()
            | Some attr ->
                attr.toMap ()
                |> Map.iter (fun k v ->
                    let camelCaseKey = Json.JsonNamingPolicy.CamelCase.ConvertName(k)

                    match v with
                    | null -> ()
                    | SomeObj(value)
                    | value -> writer.WriteAttributeString(camelCaseKey, value |> toStringFixBool))

            match node.Children with
            | Text text -> writer.WriteValue(text)
            | XmlElements children -> children |> Array.iter (fun child -> __.Write(writer, child))
            | NoElement -> ()

            writer.WriteFullEndElement()

        interface Xml.ISerializer with
            // Use different XML library ...
            member __.Serialize(o: obj) =
                try
                    let xmlNode = o :?> IXmlElement
                    use stream = new MemoryStream()
                    use writer = XmlWriter.Create(stream, settings)
                    __.Write(writer, xmlNode)
                    writer.Flush()
                    writer.Close()
                    stream.ToArray()
                with :? InvalidCastException ->
                    use stream = new MemoryStream()
                    use writer = XmlWriter.Create(stream, settings)
                    let serializer = XmlSerializer(o.GetType())
                    serializer.Serialize(writer, o, subsonicNamespace)
                    stream.ToArray()

            member __.Deserialize<'T>(xml: string) =
                let serializer = XmlSerializer(typeof<'T>)
                use reader = new StringReader(xml)
                serializer.Deserialize reader :?> 'T


    let configureServices (services: IServiceCollection) =
        services.AddGiraffe() |> ignore

        services.AddSingleton<Xml.ISerializer>(CustomXmlSerializer(writerSettings))
        |> ignore

    let log =
        LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console()
            .Enrich.FromLogContext()
            .CreateLogger()

    Log.Logger <- log

    [<EntryPoint>]
    let main args =

        "test" |> encryptPassword |> decryptPassword |> ignore

        printfn $"{makeAdmin ()}"

        Host
            .CreateDefaultBuilder()
            .ConfigureWebHostDefaults(fun webHostBuilder ->
                webHostBuilder
                    .UseUrls("http://*:8080")
                    .Configure(configureApp)
                    .ConfigureServices(configureServices)
                |> ignore)
            .Build()
            .Run()

        exitCode
