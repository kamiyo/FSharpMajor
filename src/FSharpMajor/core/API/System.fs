module FSharpMajor.API.System

open Giraffe
open Microsoft.AspNetCore.Http

open FSharpMajor.API.Types

let setXmlType: HttpHandler = setHttpHeader "Content-Type" "application/xml"

let licenseHandler: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        let serializer = ctx.GetXmlSerializer()

        let body =
            SubsonicResponse(children = XmlElements [| License(LicenseAttributes()) |])
            |> serializer.Serialize

        setBody body next ctx

let pingHandler: HttpHandler =
    fun next ctx ->
        let serializer = ctx.GetXmlSerializer()
        let body = SubsonicResponse() |> serializer.Serialize
        setBody body next ctx
