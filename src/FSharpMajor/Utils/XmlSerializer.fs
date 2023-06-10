module FSharpMajor.XmlSerializer

open System
open System.Globalization
open System.IO
open System.Text
open System.Xml
open System.Xml.Serialization
open Giraffe

open FSharpMajor.API.Types


let xmlWriterSettings =
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

let toStringFixTypes (v: obj) =
    match v with
    | :? bool -> v.ToString() |> Json.JsonNamingPolicy.CamelCase.ConvertName
    | :? DateTime -> (v :?> DateTime).ToString("o", CultureInfo.InvariantCulture)
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
                | value -> writer.WriteAttributeString(camelCaseKey, value |> toStringFixTypes))

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
