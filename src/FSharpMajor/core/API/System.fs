module FSharpMajor.API.System

open Giraffe
open Microsoft.AspNetCore.Http

open FSharpMajor.API.Types

let setXmlType: HttpHandler = setHttpHeader "Content-Type" "application/xml"

let licenseHandler: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        let serializer = ctx.GetXmlSerializer()

        let out =
            SubsonicResponse(children = XmlElements [| License(LicenseAttributes()) |])
            |> serializer.Serialize

        setBody out next ctx

let pingHandler: HttpHandler =
    fun next ctx ->
        let serializer = ctx.GetXmlSerializer()
        let out = SubsonicResponse() |> serializer.Serialize
        setBody out next ctx
