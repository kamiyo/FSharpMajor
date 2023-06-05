module API.System

open Giraffe
open Microsoft.AspNetCore.Http

open API.Types

let setXmlType : HttpHandler =
    setHttpHeader "Content-Type" "application/xml"

let licenseHandler : HttpHandler =
    setXmlType >=>
    fun (next : HttpFunc) (ctx : HttpContext) ->
        let response = {
            SubsonicResponse with Children = Some [|License|]
        }
        let serializer = ctx.GetXmlSerializer()
        let out = serializer.Serialize response
        setBody out next ctx

let pingHandler : HttpHandler =
    setXmlType >=>
    fun next ctx ->
        let response = SubsonicResponse
        let serializer = ctx.GetXmlSerializer()
        let out = serializer.Serialize response
        setBody out next ctx
