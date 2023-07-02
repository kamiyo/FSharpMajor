module FSharpMajor.API.Types

open System
open System.Reflection
open System.Xml.Serialization

open FSharp.Data
open FSharpMajor.FsLibLog


[<Literal>]
let currentVersion =
    LiteralProviders.Exec<"dotnet", "fsi ./scripts/getVersion.fsx">.Output


let subsonicNamespace = XmlSerializerNamespaces()
subsonicNamespace.Add("", "http://subsonic.org/restapi")

type BaseAttributes() =
    member __.toMap() =
        try
            [ for property in __.GetType().GetProperties(BindingFlags.Public ||| BindingFlags.Instance) ->
                  (property.Name, property.GetValue(__)) ]
            |> Map.ofSeq
        with e ->
            let logger = LogProvider.getLoggerByType typeof<BaseAttributes>
            logger.error (Log.setMessage $"Exception : {e.Message}")
            Map.empty

let upCastAttributesInOption attribute = attribute :> BaseAttributes


type ErrorAttributes(?code, ?message) =
    inherit BaseAttributes()
    member _.Code: int = defaultArg code 0
    member _.Message: string option = message


type LicenseAttributes(?valid, ?email, ?licenseExpires, ?trialExpires) =
    inherit BaseAttributes()
    member _.Valid: bool = defaultArg valid true
    member _.Email: string option = defaultArg email None
    member _.LicenseExpires: string option = licenseExpires
    member _.TrialExpires: string option = trialExpires


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
    member _.Username: string = defaultArg username ""
    member _.Email: string option = defaultArg email None
    member _.ScrobblingEnabled: bool = defaultArg scrobblingEnabled false
    member _.MaxBitRate: int option = defaultArg maxBitRate None
    member _.AdminRole: bool = defaultArg adminRole false
    member _.SettingsRole: bool = defaultArg settingsRole false
    member _.DownloadRole: bool = defaultArg downloadRole false
    member _.UploadRole: bool = defaultArg uploadRole false
    member _.PlaylistRole: bool = defaultArg playlistRole false
    member _.CoverArtRole: bool = defaultArg coverArtRole false
    member _.CommentRole: bool = defaultArg commentRole false
    member _.PodcastRole: bool = defaultArg podcastRole false
    member _.StreamRole: bool = defaultArg streamRole false
    member _.JukeboxRole: bool = defaultArg jukeboxRole false
    member _.ShareRole: bool = defaultArg shareRole false
    member _.VideoConversionRole: bool = defaultArg videoConversionRole false
    member _.AvatarLastChanged: DateTime option = defaultArg avatarLastChanged None

type MusicFolderAttributes(id, ?name) =
    inherit BaseAttributes()
    member _.Id: string = id
    member _.Name: string option = defaultArg name None

type SubsonicResponseAttributes(?xmlns, ?status, ?version, ?_attrType, ?serverVersion) =
    inherit BaseAttributes()
    member _.Xmlns: string = defaultArg xmlns "http://subsonic.org/restapi"
    member _.Status: string = defaultArg status "ok"
    member _.Version: string = defaultArg version "1.16.1"
    member _.Type: string option = (Some "fsharpmajor")

    member _.ServerVersion: string option =
        Option.orElse (Some currentVersion) serverVersion

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
    fun elements -> elements |> Seq.cast<IXmlElement> |> Array.ofSeq |> XmlElements

let optionToXmlChild opt =
    match opt with
    | Some elems -> castArrayToXmlElements elems
    | None -> NoElement

type SubsonicResponse(?attributes: SubsonicResponseAttributes, ?children) =
    interface IXmlElement with
        member _.Name = "subsonic-response"

        member _.Attributes =
            match attributes with
            | Some attr -> Some(attr :> BaseAttributes)
            | None -> Some(SubsonicResponseAttributes())

        member _.Children = defaultArg children NoElement

type Folder(child: string) =
    interface IXmlElement with
        member _.Name = "folder"
        member _.Attributes = None
        member _.Children = Text child

type License(?attributes: LicenseAttributes) =
    interface IXmlElement with
        member _.Name = "license"
        member _.Attributes = Option.map upCastAttributesInOption attributes
        member _.Children = NoElement

type User(?attributes: UserAttributes, ?children: Folder[]) =
    interface IXmlElement with
        member _.Name = "user"
        member _.Attributes = Option.map upCastAttributesInOption attributes

        member _.Children = optionToXmlChild children

type Users(?children: User[]) =
    interface IXmlElement with
        member _.Name = "users"
        member _.Attributes = None

        member _.Children = optionToXmlChild children

type MusicFolder(?attributes: MusicFolderAttributes) =
    interface IXmlElement with
        member _.Name = "musicFolder"
        member _.Attributes = Option.map upCastAttributesInOption attributes
        member _.Children = NoElement

type MusicFolders(?children: MusicFolder[]) =
    interface IXmlElement with
        member _.Name = "musicFolders"
        member _.Attributes = None
        member _.Children = optionToXmlChild children

type Error(?attributes: ErrorAttributes) =
    interface IXmlElement with
        member _.Name = "error"
        member _.Attributes = Option.map upCastAttributesInOption attributes
        member _.Children = NoElement
