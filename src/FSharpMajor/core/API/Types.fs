module API.Types

open System.Xml
open System.Reflection
open System.Xml.Serialization
open System
open Giraffe.ViewEngine.HtmlElements
open System.Runtime.Serialization

open FSharp.Data
open System.Xml
open Microsoft.FSharp.Reflection
open MBrace.FsPickler.CSharpProxy
open System.Xml
open Serilog.Context
open FsLibLog
open Giraffe.ViewEngine.HtmlElements
open Giraffe.ComputationExpressions


[<Literal>]
let currentVersion =
    LiteralProviders.Exec<"pwsh", "-f ./scripts/getVersion.ps1">.Output


let subsonicNamespace = new XmlSerializerNamespaces()
subsonicNamespace.Add("", "http://subsonic.org/restapi")



type BaseAttributes() =
    member __.toMap() =
        try
            [ for property in __.GetType().GetProperties(BindingFlags.Public ||| BindingFlags.Instance) ->
                  (property.Name, property.GetValue(__)) ]
            |> Map.ofSeq
        with e ->
            let logger = LogProvider.getLoggerByType (typeof<BaseAttributes>)
            logger.error (Log.setMessage ($"Exception : {e.Message}"))
            Map.empty

let upCastAttributesInOption attribute = attribute :> BaseAttributes


type ErrorAttributes(?code, ?message) =
    inherit BaseAttributes()
    member __.Code: int = defaultArg code 0
    member __.Message: string option = defaultArg message None


type LicenseAttributes(?valid, ?email, ?licenseExpires, ?trialExpires) =
    inherit BaseAttributes()
    member __.Valid: bool = defaultArg valid true
    member __.Email: string option = defaultArg email None
    member __.LicenseExpires: string option = defaultArg licenseExpires None
    member __.TrialExpires: string option = defaultArg trialExpires None


type UserAttributes
    (
        ?username,
        ?email,
        ?scrobblingEnabled,
        ?maxBitRate,
        ?adminRole,
        ?settingsRole,
        ?downloadRole,
        ?uploadRole,
        ?playlistRole,
        ?coverArtRole,
        ?commentRole,
        ?podcastRole,
        ?streamRole,
        ?jukeboxRole,
        ?shareRole,
        ?videoConversionRole,
        ?avatarLastChanged
    ) =
    inherit BaseAttributes()
    member __.Username: string = defaultArg username ""
    member __.Email: string option = defaultArg email None
    member __.ScrobblingEnabled: bool = defaultArg scrobblingEnabled false
    member __.MaxBitRate: int option = defaultArg maxBitRate None
    member __.AdminRole: bool = defaultArg adminRole false
    member __.SettingsRole: bool = defaultArg settingsRole false
    member __.DownloadRole: bool = defaultArg downloadRole false
    member __.UploadRole: bool = defaultArg uploadRole false
    member __.PlaylistRole: bool = defaultArg playlistRole false
    member __.CoverartRole: bool = defaultArg coverArtRole false
    member __.CommentRole: bool = defaultArg commentRole false
    member __.PodcastRole: bool = defaultArg podcastRole false
    member __.StreamRole: bool = defaultArg streamRole false
    member __.JukeboxRole: bool = defaultArg jukeboxRole false
    member __.ShareRole: bool = defaultArg shareRole false
    member __.VideoConversionRole: bool = defaultArg videoConversionRole false
    member __.AvatarLastChanged: bool option = defaultArg avatarLastChanged None

type MusicFolderAttributes(?id, ?name) =
    inherit BaseAttributes()
    member __.Id: int = defaultArg id 0
    member __.Name: string option = defaultArg name None

type SubsonicResponseAttributes(?xmlns, ?status, ?version, ?attrType, ?serverVersion) =
    inherit BaseAttributes()
    member __.Xmlns: string = defaultArg xmlns "http://subsonic.org/restapi"
    member __.Status: string = defaultArg status "ok"
    member __.Version: string = defaultArg version "1.16.1"
    member __.Type: string option = (Some "fsharpmajor")

    member __.ServerVersion: string option =
        match serverVersion with
        | Some v -> Some v
        | None -> Some currentVersion

type IXmlElement =
    abstract member Name: string
    abstract member Attributes: BaseAttributes option
    abstract member Children: XmlChild

and XmlChild =
    | Text of string
    | XmlElements of IXmlElement[]
    | NoElement

type castArrayToXmlElements<'a> = array<'a> -> XmlChild

let castArrayToXmlElements: castArrayToXmlElements<'a> =
    fun elements ->
        let casted = elements |> Seq.cast<IXmlElement> |> Array.ofSeq
        XmlElements(casted)

let optionToXmlChild opt =
    match opt with
    | Some elems -> castArrayToXmlElements elems
    | None -> NoElement

type SubsonicResponse(?attributes: SubsonicResponseAttributes, ?children) =
    interface IXmlElement with
        member __.Name = "subsonic-response"

        member __.Attributes =
            match attributes with
            | Some attr -> Some(attr :> BaseAttributes)
            | None -> Some(SubsonicResponseAttributes())

        member __.Children = defaultArg children NoElement

type Folder(child: string) =
    interface IXmlElement with
        member __.Name = "folder"
        member __.Attributes = None
        member __.Children = Text child

type License(?attributes: LicenseAttributes) =
    interface IXmlElement with
        member __.Name = "license"
        member __.Attributes = Option.map upCastAttributesInOption attributes
        member __.Children = NoElement

type User(?attributes: UserAttributes, ?children: Folder[]) =
    interface IXmlElement with
        member __.Name = "user"
        member __.Attributes = Option.map upCastAttributesInOption attributes

        member __.Children = optionToXmlChild children

type Users(?children: User[]) =
    interface IXmlElement with
        member __.Name = "users"
        member __.Attributes = None

        member __.Children = optionToXmlChild children

type MusicFolder(?attributes: MusicFolderAttributes) =
    interface IXmlElement with
        member __.Name = "musicFolder"
        member __.Attributes = Option.map upCastAttributesInOption attributes
        member __.Children = NoElement

type Musicfolders(?children: MusicFolder[]) =
    interface IXmlElement with
        member __.Name = "musicFolders"
        member __.Attributes = None
        member __.Children = optionToXmlChild children

type Error(?attributes: ErrorAttributes) =
    interface IXmlElement with
        member __.Name = "error"
        member __.Attributes = Option.map upCastAttributesInOption attributes
        member __.Children = NoElement
