module API.System

open Giraffe
open Microsoft.AspNetCore.Http

open API.Types

let setXmlType: HttpHandler = setHttpHeader "Content-Type" "application/xml"

let licenseHandler: HttpHandler =
    setXmlType
    >=> fun (next: HttpFunc) (ctx: HttpContext) ->
        let serializer = ctx.GetXmlSerializer()

        let out =
            SubsonicResponse(children = XmlElements [| License(LicenseAttributes()) |])
            |> serializer.Serialize

        setBody out next ctx

let pingHandler: HttpHandler =
    setXmlType
    >=> fun next ctx ->
        let serializer = ctx.GetXmlSerializer()
        let out = SubsonicResponse() |> serializer.Serialize
        setBody out next ctx
