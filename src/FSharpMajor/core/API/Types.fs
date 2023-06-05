module API.Types

open System.Xml
open System.Xml.Serialization
open System
open Giraffe.ViewEngine.HtmlElements
open System.Runtime.Serialization

open FSharp.Data
open System.Xml
open Microsoft.FSharp.Reflection
open MBrace.FsPickler.CSharpProxy

let [<Literal>] currentVersion = LiteralProviders.Exec<"pwsh", "-f ./FSharpMajor.fsproj">.Output

let subsonicNamespace = new XmlSerializerNamespaces()
subsonicNamespace.Add("", "http://subsonic.org/restapi")

let asAttributeMap (recd: obj) =
  [ for p in FSharpType.GetRecordFields(recd.GetType()) ->
      p.Name, p.GetValue(recd) ]
  |> Map.ofSeq

#nowarn "3535"
type IXmlAttributes = interface end

type LicenseAttributes =
    {
        Valid: bool;
        Email: string option;
        LicenseExpires: string option;
        TrialExpires: string option;
    }
    interface IXmlAttributes
type LicenseAttributes with
    static member Default() = {
        Valid = true;
        Email = None;
        LicenseExpires = None;
        TrialExpires = None;
    }


type SubsonicResponseAttributes =
    {
        Xmlns: string option;
        Status: string option;
        Version: string option;
        Type: string option;
        ServerVersion: string option;
    }
    interface IXmlAttributes

type SubsonicResponseAttributes with
    static member Default() = {
        Xmlns = Some "http://subsonic.org/restapi";
        Status = Some "ok";
        Version = Some "1.16.1";
        Type = Some "fsharpmajor";
        ServerVersion = Some currentVersion;
    }

type XmlElement =
    {
        Name: string;
        Attributes: IXmlAttributes
        Children: XmlElement[] option
    }

let License = {
    Name = "license";
    Attributes = LicenseAttributes.Default()
    Children = None
}

let SubsonicResponse = {
    Name = "subsonic-response";
    Attributes = SubsonicResponseAttributes.Default()
    Children = None
}

[<CLIMutable; XmlRoot(ElementName="folder")>]
type Folder = {
    [<XmlText>]
    Folder: int;
}

[<CLIMutable; XmlRoot(ElementName="user")>]
type User = {
    [<XmlAttribute("username")>]
    Username: string;
    [<XmlAttribute("email")>]
    Email: string option;
    [<XmlAttribute("scrobblingEnabled")>]
    ScrobblingEnabled: bool;
    [<XmlAttribute("maxBitRate")>]
    MaxBitRate: int option;
    [<XmlAttribute("adminRole")>]
    AdminRole: bool;
    [<XmlAttribute("settingsRole")>]
    SettingsRole: bool;
    [<XmlAttribute("downloadRole")>]
    DownloadRole: bool;
    [<XmlAttribute("uploadRole")>]
    UploadRole: bool;
    [<XmlAttribute("playlistRole")>]
    PlaylistRole: bool;
    [<XmlAttribute("coverartRole")>]
    CoverartRole: bool;
    [<XmlAttribute("commentRole")>]
    CommentRole: bool;
    [<XmlAttribute("podcastRole")>]
    PodcastRole: bool;
    [<XmlAttribute("streamRole")>]
    StreamRole: bool;
    [<XmlAttribute("jukeboxRole")>]
    JukeboxRole: bool;
    [<XmlAttribute("shareRole")>]
    ShareRole: bool;
    [<XmlAttribute("videoConversionRole")>]
    VideoConversionRole: bool;
    [<XmlAttribute("avatarLastChanged")>]
    AvatarLastChanged: bool;
    [<XmlArray; XmlArrayItem(ElementName="folder", IsNullable = true)>]
    Folder: Folder[];
} with
    static member Default = {
        Username = "";
        Email = None;
        ScrobblingEnabled = true;
        MaxBitRate = None;
        AdminRole = true;
        SettingsRole = true;
        DownloadRole = true;
        UploadRole = true;
        PlaylistRole = true;
        CoverartRole = true;
        CommentRole = true;
        PodcastRole = true;
        StreamRole = true;
        JukeboxRole = true;
        ShareRole = true;
        VideoConversionRole = true;
        AvatarLastChanged = true;
        Folder = [||];
    }

[<Struct>]
type Blank = struct end

// [<CLIMutable; XmlRoot(Namespace="http://subsonic.org/restapi", ElementName="subsonic-response")>]
// type SubsonicResponse<'a> = {
//     [<XmlAttribute("status")>]
//     Status: string;
//     [<XmlAttribute("version")>]
//     Version: string;
//     [<XmlAttribute("type")>]
//     Type: string;
//     [<XmlAttribute("serverVersion")>]
//     ServerVersion: string;
//     [<XmlArray(IsNullable = true)>]
//     [<XmlArrayItem(ElementName="license", IsNullable=true, Type=typeof<License>)>]
//     [<XmlArrayItem(ElementName="user", IsNullable=true, Type=typeof<User>)>]
//     Body: 'a[];
// } with
//     static member Default() = {
//         Status = "ok";
//         Version = "1.16.1";
//         Type = "fsharpmajor";
//         ServerVersion = "0.1.1";
//         Body = [||];
//     }