module FSharpMajor.Tests.XmlResponseTests

open System
open System.IO
open System.Net
open System.Text
open System.Xml
open System.Xml.Schema
open FSharpMajor.API.Error
open FSharpMajor.Authorization
open FSharpMajor.DatabaseService
open FSharpMajor.XmlSerializer
open Giraffe
open FsConfig
open FSharpMajor
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.TestHost
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.WebUtilities
open Microsoft.Extensions.DependencyInjection
open System.Security.Cryptography
open System.Net.Http
open Xunit
open Xunit.Abstractions
open dotenv.net

DotEnv.Load(DotEnvOptions(envFilePaths = [| ".env" |], probeForEnv = true, probeLevelsToSearch = 8))

[<Convention("ADMIN")>]
type Config = { User: string; Password: string }

let getTestHost () =
    WebHostBuilder()
        .UseTestServer()
        .Configure(Action<IApplicationBuilder> Program.configureApp)
        .ConfigureServices(fun services ->
            services.AddGiraffe() |> ignore

            services.AddScoped<IDatabaseService, DatabaseService>() |> ignore

            services
                .AddAuthentication("Basic")
                .AddScheme<BasicAuthenticationOptions, BasicAuthHandler>("Basic", null)
            |> ignore

            services.AddScoped<IAuthenticationManager, SubsonicAuthenticationManager>()
            |> ignore

            services.AddSingleton<Xml.ISerializer>(CustomXmlSerializer(xmlWriterSettings))
            |> ignore)
        .UseUrls("http://*:8080")

let testRequest (request: HttpRequestMessage) =
    let resp =
        task {
            use server = new TestServer(getTestHost ())
            use client = server.CreateClient()
            let! response = request |> client.SendAsync
            return response
        }

    resp.Result

let testRequestWithAuth (url: string) (queries: (string * string) list) (output: ITestOutputHelper) =
    match EnvConfig.Get<Config>() with
    | Error _ -> failwith "Config failed"
    | Ok config ->
        let salt = RandomNumberGenerator.GetHexString(16, true)

        let tokensalt =
            (config.Password + salt)
            |> UTF8Encoding.UTF8.GetBytes
            |> MD5.HashData
            |> Convert.ToHexString
            |> (_.ToLower())
            
        let authQueries = 
                    [ ("u", config.User)
                      ("s", salt)
                      ("t", tokensalt)
                      ("v", "1.16.1")
                      ("c", "test") ]

        let url =
            QueryHelpers.AddQueryString(
                url,
                List.concat [queries; authQueries] |> Map.ofList
            )
            
        let request = new HttpRequestMessage(HttpMethod.Get, url)

        output.WriteLine(request.ToString())
        testRequest request

type XmlValidator() =
    member val errors = 0 with get, set
    member val warnings = 0 with get, set

    member this.validateData(msg, severity) =
        if severity = XmlSeverityType.Error then
            this.errors <- this.errors + 1
            printfn $"Validation error: %s{msg}"
        else
            this.warnings <- this.warnings + 1
            printfn $"Validation warning: %s{msg}"


    member this.clear() =
        this.errors <- 0
        this.warnings <- 0

let xmlValidator = XmlValidator()
let xmlSettings = XmlReaderSettings()

xmlSettings.Schemas.Add("http://subsonic.org/restapi", "../../../../subsonic-rest-api-1.16.1.xsd")
|> ignore

xmlSettings.ValidationType <- ValidationType.Schema

let xmlValidationEventHandler =
    ValidationEventHandler(fun _ e -> xmlValidator.validateData (e.Message, e.Severity))

xmlSettings.ValidationEventHandler.AddHandler(xmlValidationEventHandler)

xmlSettings.ValidationFlags <-
    xmlSettings.ValidationFlags
    ||| XmlSchemaValidationFlags.ProcessIdentityConstraints
    ||| XmlSchemaValidationFlags.ReportValidationWarnings

let validateError (xml: string) (error: ErrorEnum) =
    xmlValidator.clear ()
    let reader = XmlReader.Create(new StringReader(xml), xmlSettings)

    while reader.Read() do
        match reader.NodeType with
        | XmlNodeType.Element when reader.Name = "error" ->
            Assert.Equal((error |> int).ToString(), reader.GetAttribute("code"))
        | _ -> ()

let validateResponse (xml: string) =
    xmlValidator.clear ()
    let reader = XmlReader.Create(new StringReader(xml), xmlSettings)

    while reader.Read() do
        ()

type APITest(output: ITestOutputHelper) =
    
    member this.checkAndValidate (response: HttpResponseMessage) =
        let content = response.Content.ReadAsStringAsync().Result
        Assert.Equal(HttpStatusCode.OK, response.StatusCode)
        validateResponse content
    
    [<Fact>]
    member this.``Test missing params error``() =
        let response = testRequest (new HttpRequestMessage(HttpMethod.Get, "/rest/ping.view"))

        let content = response.Content.ReadAsStringAsync().Result
        Assert.Equal(response.StatusCode, HttpStatusCode.OK)
        validateError content ErrorEnum.Params

    [<Fact>]
    member this.``Test wrong credentials``() =
        let url =
            QueryHelpers.AddQueryString(
                "/rest/ping.view",
                Map<string, string>(
                    [ ("u", "test")
                      ("s", "000000")
                      ("t", "000000")
                      ("v", "1.16.1")
                      ("c", "test") ]
                )
            )

        let request = new HttpRequestMessage(HttpMethod.Get, url)

        let response = testRequest request

        let content = response.Content.ReadAsStringAsync().Result
        Assert.Equal(HttpStatusCode.OK, response.StatusCode)
        validateError content ErrorEnum.Credentials

    [<Fact>]
    member this.``Test ping``() =
        testRequestWithAuth "/rest/ping.view" [] this.output
        |> this.checkAndValidate
        
    [<Fact>]
    member this.``Test license``() =
        testRequestWithAuth "/rest/getLicense.view" [] this.output
        |> this.checkAndValidate
        
    [<Fact>]
    member this.``Test getMusicFolders``() =
        testRequestWithAuth "/rest/getMusicFolders.view" [] this.output
        |> this.checkAndValidate
        
    [<Fact>]
    member this.``Test getUser``() =
        testRequestWithAuth "/rest/getUser.view" [] this.output
        |> this.checkAndValidate
        
    [<Fact>]
    member this.``Test getIndexes``() =
        testRequestWithAuth "/rest/getIndexes.view" [] this.output
        |> this.checkAndValidate
        
        let musicFolderResponse = testRequestWithAuth "/rest/getMusicFolders.view" [] this.output
        let content = musicFolderResponse.Content.ReadAsStringAsync().Result
        xmlValidator.clear ()
        let reader = XmlReader.Create(new StringReader(content), xmlSettings)

        let mutable id = ""
        while reader.Read() && id = "" do
            match reader.NodeType with
            | XmlNodeType.Element when reader.Name = "musicFolder" ->
                id <- reader.GetAttribute("id")
            | _ -> ()
        
        testRequestWithAuth "/rest/getIndexes.view" [("musicFolderId", id)] this.output
        |> this.checkAndValidate

    

    member private _.output: ITestOutputHelper = output
