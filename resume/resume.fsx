#if INTERACTIVE
#r "nuget: Avalonia.Desktop, 11.3.13"
#r "nuget: Avalonia.Themes.Fluent, 11.3.13"
#r "nuget: Avalonia.FuncUI, 1.5.2"
#r "nuget: FluentIcons.Avalonia, 2.0.321"
#r "nuget: MessageBox.Avalonia, 3.3.1.1"
#r "nuget: PuppeteerSharp, 24.40.0"
#r "nuget: Markdig, 1.1.2"
#endif

open System
open System.Collections.ObjectModel
open System.IO
open System.Text.RegularExpressions
open System.Xml.Linq
open System.Xml.Xsl
open System.Web
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Hosts
open Avalonia.Layout
open Avalonia.Media
open Avalonia.Media.Imaging
open Avalonia.Platform.Storage
open Avalonia.Styling
open FluentIcons.Avalonia
open PuppeteerSharp
open PuppeteerSharp.Media
open Markdig
open MsBox.Avalonia
open MsBox.Avalonia.Enums
open FluentIcons.Common
open System.Runtime.InteropServices

type GenerateToFormat =
    | Pdf
    | Xml
    | Html

type XsltFile(name: string, path: string) =
    member val Name = name with get, set
    member val Path = path with get, set

type MdTransform() =
    member _.ConvertToHtml(text: string) =
        let pipeline = MarkdownPipelineBuilder().UseAdvancedExtensions().Build()
        Markdown.ToHtml(text, pipeline)

[<Serializable>]
type Education
    (school: string, degree: string, area: string, grade: string, location: string, period: string, website: string) =
    member val School = school with get, set
    member val Degree = degree with get, set
    member val Area = area with get, set
    member val Grade = grade with get, set
    member val Location = location with get, set
    member val Period = period with get, set
    member val Website = website with get, set

[<Serializable>]
type Experience(company: string, website: Uri, position: string, location: string, period: string, description: string)
    =
    member val Company = company with get, set
    member val Website = website with get, set
    member val Position = position with get, set
    member val Location = location with get, set
    member val Period = period with get, set
    member val Description = description with get, set

[<Serializable>]
type Language(name: string, fluency: string, level: int) =
    member val Name = name with get, set
    member val Fluency = fluency with get, set
    member val Level = level with get, set

[<Serializable>]
type Skill(name: string, keywords: string list) =
    member val Name = name with get, set
    member val Keywords = keywords with get, set

[<Serializable>]
type Certification(title: string, issuer: string, date: string, label: string, website: Uri) =
    member val Title = title with get, set
    member val Issuer = issuer with get, set
    member val Date = date with get, set
    member val Label = label with get, set
    member val Website = website with get, set

let getCurrentDirectory =
    #if INTERACTIVE
    __SOURCE_DIRECTORY__
    #endif
    #if COMPILED
    AppContext.BaseDirectory
    #endif

let emailRegex = Regex("^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)
let phoneRegex = Regex("^\+?[0-9\s\-()]+$", RegexOptions.Compiled)

[<AutoOpen>]
module SymbolIcon =
    open Avalonia.FuncUI.Types
    open Avalonia.FuncUI.Builder
    open FluentIcons.Avalonia
    open FluentIcons.Common

    let create (attrs: IAttr<SymbolIcon> list) : IView<SymbolIcon> = ViewBuilder.Create<SymbolIcon> attrs

    type SymbolIcon with
        static member symbol<'t when 't :> SymbolIcon>(value: Symbol) : IAttr<'t> =
            AttrBuilder<'t>
                .CreateProperty<Symbol>(SymbolIcon.SymbolProperty, value, ValueNone)

        static member iconVariant<'t when 't :> SymbolIcon>(value: IconVariant) : IAttr<'t> =
            AttrBuilder<'t>
                .CreateProperty<IconVariant>(SymbolIcon.IconVariantProperty, value, ValueNone)

let isValidUrl (url: string) =
    try
        let uri = new Uri(url)

        (uri.Scheme = Uri.UriSchemeHttp || uri.Scheme = Uri.UriSchemeHttps)
        && uri.IsAbsoluteUri
    with
    | :? UriFormatException -> false
    | _ -> false

let getMainWindow () =
    match Application.Current.ApplicationLifetime with
    | :? ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime as desktop -> desktop.MainWindow
    | _ -> null

let showErrorMsgBoxAsync(text: string) =
    async {
        let box = 
            MessageBoxManager.GetMessageBoxStandard("Error", 
                text, 
                ButtonEnum.Ok, Icon.Error)  
        return box.ShowAsync()
    }

let choosePicturePath () =
    async {
        let win = getMainWindow ()

        if isNull win then
            return None
        else
            let options = FilePickerOpenOptions()
            options.AllowMultiple <- false
            options.Title <- "Choose a picture"

            options.FileTypeFilter <-
                [ "*.jpg"; "*.jpeg"; "*.png"; "*.bmp"; "*.gif" ]
                |> List.map (fun pattern -> FilePickerFileType pattern)

            let! res = win.StorageProvider.OpenFilePickerAsync options |> Async.AwaitTask

            if Seq.isEmpty res then
                return None
            else
                return Some (Seq.head res).Path
    }

let chooseFile (format: GenerateToFormat) =
    async {
        let win = getMainWindow ()

        if isNull win then
            return None
        else
            let saveOptions =
                let options = FilePickerSaveOptions()
                options.Title <- sprintf "Save resume as a %A file" format
                options.ShowOverwritePrompt <- true

                options.DefaultExtension <-
                    match format with
                    | GenerateToFormat.Xml -> "xml"
                    | GenerateToFormat.Html -> "html"
                    | GenerateToFormat.Pdf -> "pdf"

                options.SuggestedFileName <- sprintf "resume.%s" options.DefaultExtension

                let fileType =
                    match format with
                    | GenerateToFormat.Xml ->
                        FilePickerFileType "XML"
                        |> fun x ->
                            x.Patterns <- [| "*.xml" |]
                            x
                    | GenerateToFormat.Html ->
                        FilePickerFileType "HTML"
                        |> fun x ->
                            x.Patterns <- [| "*.html" |]
                            x
                    | GenerateToFormat.Pdf ->
                        FilePickerFileType "PDF"
                        |> fun x ->
                            x.Patterns <- [| "*.pdf" |]
                            x

                options.SuggestedFileType <- fileType
                options

            use! file = win.StorageProvider.SaveFilePickerAsync saveOptions |> Async.AwaitTask
            return if isNull file then None else Some file.Path.LocalPath
    }

let getXsltFiles =
    let folder = Path.Combine(getCurrentDirectory, "xslt") |> DirectoryInfo

    if folder.Exists then
        folder.GetFiles "*.xslt"
        |> Array.map (fun f -> XsltFile(Path.GetFileNameWithoutExtension f.Name, f.FullName))
    else
        Array.empty

let chooseXmlPath () =
    async {
        let win = getMainWindow ()

        if isNull win then
            return None
        else
            let options = FilePickerOpenOptions()
            options.AllowMultiple <- false
            options.Title <- "Choose resume XML"

            options.FileTypeFilter <-
                [ FilePickerFileType("XML")
                  |> fun x ->
                      x.Patterns <- [| "*.xml" |]
                      x ]

            let! res = win.StorageProvider.OpenFilePickerAsync options |> Async.AwaitTask

            if Seq.isEmpty res then
                return None
            else
                return Some (Seq.head res).Path.LocalPath
    }

let tryMakeUri (s: string) : Uri option =
    if String.IsNullOrWhiteSpace s then
        None
    else
        try
            let u = Uri(s, UriKind.RelativeOrAbsolute)

            if not u.IsAbsoluteUri && File.Exists(s) then
                Some(Uri(Path.GetFullPath s))
            else
                Some u
        with _ ->
            if File.Exists(s) then
                Some(Uri(Path.GetFullPath s))
            else
                None

let getExperiencesXml (experiences: ObservableCollection<Experience>) =
    experiences
    |> Seq.map (fun exp ->
        XElement(
            "experience",
            XElement("company", exp.Company),
            XElement(
                "website",
                if exp.Website = null then
                    ""
                else
                    exp.Website.OriginalString
            ),
            XElement("position", exp.Position),
            XElement("location", exp.Location),
            XElement("period", exp.Period),
            XElement("description", exp.Description)
        ))
    |> Seq.toArray

let getLanguagesXml (languages: ObservableCollection<Language>) =
    languages
    |> Seq.map (fun lang ->
        XElement(
            "language",
            XElement("name", lang.Name),
            XElement("fluency", lang.Fluency),
            XElement("level", lang.Level)
        ))
    |> Seq.toArray

let getSkillsXml (skills: ObservableCollection<Skill>) =
    skills
    |> Seq.map (fun skill ->
        XElement(
            "skill",
            XElement("name", skill.Name),
            XElement("keywords", skill.Keywords |> List.map (fun kw -> XElement("keyword", kw)) |> Seq.toArray)
        ))
    |> Seq.toArray

let getCertificationsXml (certifications: ObservableCollection<Certification>) =
    certifications
    |> Seq.map (fun cert ->
        XElement(
            "certification",
            XElement("title", cert.Title),
            XElement("issuer", cert.Issuer),
            XElement("date", cert.Date),
            XElement("label", cert.Label),
            XElement(
                "website",
                if cert.Website = null then
                    ""
                else
                    cert.Website.OriginalString
            )
        ))
    |> Seq.toArray

let getXmlDoc(picturePath: string, name: string, headline: string, email: string,
    phone: string, location: string, links: ObservableCollection<string>, summary: string,
    experiences: ObservableCollection<Experience>, languages: ObservableCollection<Language>,
    skills: ObservableCollection<Skill>, certifications: ObservableCollection<Certification>,
    educations: ObservableCollection<Education>) =
    let embedPicture (path: string) : string =
        if String.IsNullOrWhiteSpace path then
            ""
        else
            try
                let uri = Uri path
                let filePath = if uri.IsFile then uri.LocalPath else path

                if File.Exists filePath then
                    let bytes = File.ReadAllBytes filePath

                    let mime =
                        match Path.GetExtension(filePath).ToLowerInvariant() with
                        | ".jpg"
                        | ".jpeg" -> "image/jpeg"
                        | ".png" -> "image/png"
                        | ".gif" -> "image/gif"
                        | ".bmp" -> "image/bmp"
                        | _ -> "application/octet-stream"

                    let base64 = Convert.ToBase64String(bytes)
                    sprintf "data:%s;base64,%s" mime base64
                else
                    path
            with _ ->
                path

    let pictureValue = embedPicture picturePath

    let doc =
        XDocument(
            XElement(
                "resume",
                XElement("picturePath", pictureValue),
                XElement("name", name),
                XElement("headline", headline),
                XElement("email", email),
                XElement("phone", phone),
                XElement("location", location),
                XElement("links", links |> Seq.map (fun l -> XElement("link", l)) |> Seq.toArray),
                XElement("summary", summary),
                XElement("experiences", getExperiencesXml experiences),
                XElement("languages", getLanguagesXml languages),
                XElement("skills", getSkillsXml skills),
                XElement("certifications", getCertificationsXml certifications),
                XElement(
                    "educations",
                    educations
                    |> Seq.map (fun edu ->
                        XElement(
                            "education",
                            XElement("school", edu.School),
                            XElement("degree", edu.Degree),
                            XElement("area", edu.Area),
                            XElement("grade", edu.Grade),
                            XElement("location", edu.Location),
                            XElement("period", edu.Period),
                            XElement("website", edu.Website)
                        ))
                    |> Seq.toArray
                )
            )
        )

    doc

let transformXmlToHtml (doc: XDocument, xsltPath: string) =
    try
        let args = new XsltArgumentList()
        args.AddExtensionObject("urn:ExtObj", new MdTransform())
        let xslt = new XslCompiledTransform()
        xslt.Load xsltPath
        use stringWriter = new StringWriter()
        use docReader = doc.CreateReader()
        xslt.Transform(docReader, args, stringWriter)
        Some (stringWriter.ToString())
    with 
         | :? XsltException as ex ->
            showErrorMsgBoxAsync $"Failed to transform XSLT '{xsltPath}': {ex.Message}, Line number: {ex.LineNumber}, Line position: {ex.LinePosition}" 
            |> Async.StartImmediateAsTask |> Async.AwaitTask |> ignore
            None 
         | ex ->   
            showErrorMsgBoxAsync $"Failed to transform XSLT '{xsltPath}': {ex.Message}"  
            |> Async.StartImmediateAsTask |> Async.AwaitTask |> ignore
            None             

let findChromePath =
    let commonPaths =
        if RuntimeInformation.IsOSPlatform OSPlatform.Windows then
            [ @"C:\Program Files\Google\Chrome\Application\chrome.exe"
              @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"
              Environment.GetFolderPath Environment.SpecialFolder.LocalApplicationData
              + @"\Google\Chrome\Application\chrome.exe" ]
        elif RuntimeInformation.IsOSPlatform OSPlatform.Linux then
            [ "/usr/bin/google-chrome"
              "/usr/bin/chromium"
              "/usr/bin/chromium-browser"
              "/usr/bin/google-chrome-stable" ]
        elif RuntimeInformation.IsOSPlatform OSPlatform.OSX then
            [ @"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
              Environment.GetFolderPath Environment.SpecialFolder.UserProfile
              + @"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" ]
        else
            []

    commonPaths |> List.tryFind File.Exists

let launchOptions =
    task {
        let chromePath = findChromePath

        let options =
            LaunchOptions(Headless = true, Args = [| "--no-sandbox"; "--disable-setuid-sandbox" |])

        match chromePath with
        | Some path ->
            options.ExecutablePath <- path
            return options
        | None ->
            let browserFetcher = new BrowserFetcher()
            let! installedBrowser = browserFetcher.DownloadAsync()
            options.ExecutablePath <- installedBrowser.GetExecutablePath()
            return options
    }    
            
let generatePdfFromHtml (htmlContent: string, outputPath: string) =
    task {
        let! options = launchOptions
        use! browser = Puppeteer.LaunchAsync options
        use! page = browser.NewPageAsync()
        do! page.SetContentAsync htmlContent

        let pdfOptions =
            PdfOptions(
                Format = PaperFormat.A4,
                DisplayHeaderFooter = false,
                PrintBackground = true,
                MarginOptions = new MarginOptions(Top = "0mm", Bottom = "0mm", Left = "0mm", Right = "0mm")
            )

        do! page.PdfAsync(outputPath, pdfOptions)
    }

type LoadStates =
    { pictureUriState: IWritable<Uri option>
      nameState: IWritable<string>
      headlineState: IWritable<string>
      emailState: IWritable<string>
      phoneState: IWritable<string>
      locationState: IWritable<string>
      linksState: IWritable<ObservableCollection<string>>
      summaryState: IWritable<string>
      experiencesState: IWritable<ObservableCollection<Experience>>
      languagesState: IWritable<ObservableCollection<Language>>
      skillsState: IWritable<ObservableCollection<Skill>>
      certificationsState: IWritable<ObservableCollection<Certification>>
      educationsState: IWritable<ObservableCollection<Education>> }

let loadFromXml (states: LoadStates) =
    async {
        let! opt = chooseXmlPath ()

        match opt with
        | Some path ->
            try
                let doc = XDocument.Load path

                let get name =
                    let el = doc.Root.Element(XName.Get name)
                    if isNull el then "" else el.Value

                // picture
                let pic = get "picturePath"

                if not (String.IsNullOrWhiteSpace pic) then
                    states.pictureUriState.Set(tryMakeUri pic)
                else
                    states.pictureUriState.Set None

                // basic info
                states.nameState.Set(get "name")
                states.headlineState.Set(get "headline")
                states.emailState.Set(get "email")
                states.phoneState.Set(get "phone")
                states.locationState.Set(get "location")

                // links
                states.linksState.Current.Clear()
                let linksEl = doc.Root.Element(XName.Get "links")

                if not (isNull linksEl) then
                    for linkNode in linksEl.Elements(XName.Get "link") do
                        states.linksState.Current.Add linkNode.Value

                // summary
                states.summaryState.Set(get "summary")

                // experiences
                states.experiencesState.Current.Clear()
                let expEl = doc.Root.Element(XName.Get "experiences")

                if not (isNull expEl) then
                    for expNode in expEl.Elements(XName.Get "experience") do
                        let company =
                            expNode.Element(XName.Get "company")
                            |> fun e -> if isNull e then "" else e.Value

                        let website =
                            expNode.Element(XName.Get "website")
                            |> fun e -> if isNull e then "" else e.Value

                        let position =
                            expNode.Element(XName.Get "position")
                            |> fun e -> if isNull e then "" else e.Value

                        let location =
                            expNode.Element(XName.Get "location")
                            |> fun e -> if isNull e then "" else e.Value

                        let period =
                            expNode.Element(XName.Get "period") |> fun e -> if isNull e then "" else e.Value

                        let description =
                            expNode.Element(XName.Get "description")
                            |> fun e -> if isNull e then "" else e.Value

                        let uri =
                            if String.IsNullOrWhiteSpace website then
                                null
                            else
                                Uri(website)

                        states.experiencesState.Current.Add(
                            Experience(company, uri, position, location, period, description)
                        )

                // languages
                states.languagesState.Current.Clear()
                let langEl = doc.Root.Element(XName.Get "languages")

                if not (isNull langEl) then
                    for langNode in langEl.Elements(XName.Get "language") do
                        let name =
                            langNode.Element(XName.Get "name") |> fun e -> if isNull e then "" else e.Value

                        let fluency =
                            langNode.Element(XName.Get "fluency")
                            |> fun e -> if isNull e then "" else e.Value

                        let level =
                            langNode.Element(XName.Get "level")
                            |> fun e -> if isNull e then 0 else Int32.Parse e.Value

                        states.languagesState.Current.Add(Language(name, fluency, level))

                // skills
                states.skillsState.Current.Clear()
                let skillsEl = doc.Root.Element(XName.Get "skills")

                if not (isNull skillsEl) then
                    for skillNode in skillsEl.Elements(XName.Get "skill") do
                        let name =
                            skillNode.Element(XName.Get "name") |> fun e -> if isNull e then "" else e.Value

                        let keywords =
                            let kwEl = skillNode.Element(XName.Get "keywords")

                            if isNull kwEl then
                                []
                            else
                                kwEl.Elements(XName.Get "keyword") |> Seq.map (fun kw -> kw.Value) |> Seq.toList

                        states.skillsState.Current.Add(Skill(name, keywords))

                // certifications
                states.certificationsState.Current.Clear()
                let certEl = doc.Root.Element(XName.Get "certifications")

                if not (isNull certEl) then
                    for certNode in certEl.Elements(XName.Get "certification") do
                        let title =
                            certNode.Element(XName.Get "title") |> fun e -> if isNull e then "" else e.Value

                        let issuer =
                            certNode.Element(XName.Get "issuer")
                            |> fun e -> if isNull e then "" else e.Value

                        let date =
                            certNode.Element(XName.Get "date") |> fun e -> if isNull e then "" else e.Value

                        let label =
                            certNode.Element(XName.Get "label") |> fun e -> if isNull e then "" else e.Value

                        let website =
                            certNode.Element(XName.Get "website")
                            |> fun e -> if isNull e then "" else e.Value

                        let uri =
                            if String.IsNullOrWhiteSpace website then
                                null
                            else
                                Uri(website)

                        states.certificationsState.Current.Add(Certification(title, issuer, date, label, uri))

                // educations
                states.educationsState.Current.Clear()
                let eduEl = doc.Root.Element(XName.Get "educations")

                if not (isNull eduEl) then
                    for eduNode in eduEl.Elements(XName.Get "education") do
                        let school =
                            eduNode.Element(XName.Get "school") |> fun e -> if isNull e then "" else e.Value

                        let degree =
                            eduNode.Element(XName.Get "degree") |> fun e -> if isNull e then "" else e.Value

                        let area =
                            eduNode.Element(XName.Get "area") |> fun e -> if isNull e then "" else e.Value

                        let grade =
                            eduNode.Element(XName.Get "grade") |> fun e -> if isNull e then "" else e.Value

                        let location =
                            eduNode.Element(XName.Get "location")
                            |> fun e -> if isNull e then "" else e.Value

                        let period =
                            eduNode.Element(XName.Get "period") |> fun e -> if isNull e then "" else e.Value

                        let website =
                            eduNode.Element(XName.Get "website")
                            |> fun e -> if isNull e then "" else e.Value

                        states.educationsState.Current.Add(
                            Education(school, degree, area, grade, location, period, website)
                        )

            with ex ->
                let! result = showErrorMsgBoxAsync $"Failed to load XML '{path}': {ex.Message}"
                result |> ignore
        | None -> ()
    }
    |> Async.StartImmediate

let pictureTabConent (imgSource: Bitmap, pictureUriState: IWritable<option<Uri>>) =
    StackPanel.create [ 
            StackPanel.orientation Orientation.Vertical
            StackPanel.verticalAlignment VerticalAlignment.Top            
            StackPanel.children [
                Image.create [Image.source imgSource;Image.maxHeight 120.0;Image.maxWidth 120.0 ]
                TextBox.create [ 
                    TextBox.watermark "Image path"
                    TextBox.height 30.0
                    TextBox.text (
                        pictureUriState.Current
                        |> Option.map (fun u -> u.OriginalString)
                        |> Option.defaultValue "" )
                    TextBox.onTextChanged (fun t ->
                        match tryMakeUri t with
                        | Some u -> pictureUriState.Set(Some u)
                        | None -> pictureUriState.Set None)]
                Button.create [
                    Button.content "Choose picture"
                    Button.horizontalAlignment HorizontalAlignment.Right
                    Button.onClick (fun _ ->
                        async {
                            let! opt = choosePicturePath ()
                            opt |> Option.iter(fun p -> pictureUriState.Set(Some p))
                        }
                        |> Async.StartImmediate)                     
                ] ] ]    

let basicInfoTabContent (name: IWritable<string>, 
    headline: IWritable<string>, email: IWritable<string>, 
    phone: IWritable<string>, location: IWritable<string>,
    links: IWritable<ObservableCollection<string>>, selectedLink: IWritable<string>,
    newLink: IWritable<string>, selectedLinkIndex: IWritable<int>) =
    StackPanel.create [
        StackPanel.orientation Orientation.Vertical
        StackPanel.children [
            TextBox.create [ 
                TextBox.watermark "Name"
                TextBox.text name.Current
                TextBox.onTextChanged (fun t -> name.Set t) ]
            TextBox.create [ 
                TextBox.watermark "Headline"
                TextBox.text headline.Current
                TextBox.onTextChanged (fun t -> headline.Set t) ]
            TextBox.create [ 
                TextBox.watermark "Email"
                TextBox.text email.Current
                TextBox.onTextChanged (fun t -> email.Set t)
                if email.Current <> "" && not (emailRegex.IsMatch email.Current)
                then TextBox.classes [ "invalid" ] ]
            TextBox.create [
                TextBox.watermark "Phone"
                TextBox.text phone.Current
                TextBox.onTextChanged (fun t -> phone.Set t)
                if phone.Current <> "" && not (phoneRegex.IsMatch phone.Current)
                then TextBox.classes [ "invalid" ] ]
            TextBox.create [ 
                TextBox.watermark "Location"
                TextBox.text location.Current
                TextBox.onTextChanged (fun t -> location.Set t) ]
            TextBlock.create [ 
                TextBlock.text "Links:" ]
            ListBox.create [ 
                ListBox.dataItems links.Current
                ListBox.selectionMode SelectionMode.Single
                ListBox.selectedItem selectedLink.Current
                ListBox.onSelectedIndexChanged (fun index -> selectedLinkIndex.Set index)
                ListBox.onSelectedItemChanged (fun item ->
                        newLink.Set(item |> string)
                        selectedLink.Set(item |> string)) ]
            TextBox.create [ 
                TextBox.watermark "New link"
                TextBox.horizontalAlignment HorizontalAlignment.Stretch
                TextBox.text newLink.Current
                TextBox.onTextChanged (fun t -> newLink.Set t)
                if newLink.Current <> "" && not (isValidUrl newLink.Current)
                then TextBox.classes [ "invalid" ] ]
            StackPanel.create [ 
                StackPanel.horizontalAlignment HorizontalAlignment.Right
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [ 
                    Button.create [ 
                        Button.content "Add"
                        StackPanel.classes ["standart"]
                        Button.onClick (fun _ ->
                                if not ( String.IsNullOrWhiteSpace newLink.Current)
                                    && isValidUrl newLink.Current
                                    && links.Current.Contains newLink.Current |> not
                                then
                                    links.Current.Add newLink.Current
                                    newLink.Set"") ]
                    Button.create [ 
                        Button.content "Update"
                        StackPanel.classes ["standart"]
                        Button.isEnabled (not (String.IsNullOrEmpty selectedLink.Current))
                        Button.onClick (fun _ ->
                                if links.Current.Count > 0
                                    && selectedLinkIndex.Current >= 0
                                then
                                    links.Current.[selectedLinkIndex.Current] <- newLink.Current) ]
                    Button.create [ 
                        Button.content "Delete"
                        StackPanel.classes ["standart"]
                        Button.isEnabled (not (String.IsNullOrEmpty selectedLink.Current))
                        Button.onClick (fun _ ->
                                if links.Current.Contains selectedLink.Current
                                then
                                    if links.Current.Remove selectedLink.Current
                                    then selectedLink.Set "") ] ] ]
                ] ]    

let summaryTabContent (summary: IWritable<string>) =
    TextBox.create [ 
        TextBox.watermark "Summary (markdown)"
        TextBox.margin 10.0
        TextBox.padding 10.0
        TextBox.verticalAlignment VerticalAlignment.Top
        TextBox.text summary.Current
        TextBox.onTextChanged (fun t -> summary.Set t)
        TextBox.acceptsReturn true
        TextBox.height 370.0
        TextBox.verticalScrollBarVisibility ScrollBarVisibility.Visible
        TextBox.horizontalScrollBarVisibility ScrollBarVisibility.Visible ]    

let experienceTabContent (experiences: IWritable<ObservableCollection<Experience>>, selectedIndex: IWritable<int>,
    company: IWritable<string>, position: IWritable<string>, location: IWritable<string>,
    period: IWritable<string>, description: IWritable<string>, website: IWritable<string>) =
    StackPanel.create [ 
        StackPanel.orientation Orientation.Vertical
        StackPanel.children [ 
            ListBox.create [ 
                ListBox.dataItems experiences.Current
                ListBox.selectionMode SelectionMode.Single
                ListBox.onSelectionChanged (fun args ->
                if args.AddedItems.Count > 0
                then
                    let exp = args.AddedItems.[0]:?> Experience
                    selectedIndex.Set(experiences.Current.IndexOf exp)
                    company.Set exp.Company
                    position.Set exp.Position
                    location.Set exp.Location
                    period.Set exp.Period
                    description.Set exp.Description
                    website.Set(
                        if exp.Website <> null
                        then exp.Website.OriginalString
                        else "")
                else
                    selectedIndex.Set -1
                    company.Set ""
                    position.Set ""
                    location.Set ""
                    period.Set ""
                    description.Set ""
                    website.Set "")
                ListBox.itemTemplate (
                    DataTemplateView<_>.create(fun (exp: Experience) ->
                            TextBlock.create [ 
                                TextBlock.text (sprintf "%s - %s" exp.Company exp.Position) ])
                        ) ]
            TextBox.create [ 
                TextBox.watermark "Company"
                TextBox.text company.Current
                TextBox.onTextChanged (fun t -> company.Set t) ]
            TextBox.create [ 
                TextBox.watermark "Position"
                TextBox.text position.Current
                TextBox.onTextChanged (fun t -> position.Set t) ]
            TextBox.create [ 
                TextBox.watermark "Location"
                TextBox.text location.Current
                TextBox.onTextChanged (fun t -> location.Set t) ]
            TextBox.create [ 
                TextBox.watermark "Period"
                TextBox.text period.Current
                TextBox.onTextChanged (fun t -> period.Set t) ]
            TextBox.create [ 
                TextBox.watermark "Website"
                TextBox.text website.Current
                TextBox.onTextChanged (fun t -> website.Set t)
                if website.Current <> "" && not ( isValidUrl website.Current )
                then TextBox.classes [ "invalid" ] ]
            TextBox.create [ 
                TextBox.watermark "Description (markdown)"
                TextBox.text description.Current
                TextBox.onTextChanged (fun t -> description.Set t)
                TextBox.acceptsReturn true
                TextBox.height 80.0
                TextBox.verticalScrollBarVisibility ScrollBarVisibility.Auto ]
            StackPanel.create [ 
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Right
                StackPanel.children [ 
                    Button.create [ 
                        Button.content "Add"
                        StackPanel.classes ["standart"]
                        Button.onClick (fun _ ->
                            if not (String.IsNullOrWhiteSpace company.Current)
                            then
                                let websiteUri =
                                    if String.IsNullOrWhiteSpace website.Current
                                    then null
                                    else Uri website.Current

                                let exp =
                                    Experience(
                                        company.Current,
                                        websiteUri,
                                        position.Current,
                                        location.Current,
                                        period.Current,
                                        description.Current
                                    )

                                experiences.Current.Add exp
                                company.Set ""
                                position.Set ""
                                location.Set ""
                                period.Set ""
                                description.Set ""
                                website.Set "") ]
                    Button.create [ 
                        Button.content "Update"
                        StackPanel.classes ["standart"]
                        Button.isEnabled (selectedIndex.Current >= 0)
                        Button.onClick (fun _ ->
                            if selectedIndex.Current >= 0
                            then
                                let exp = experiences.Current.[selectedIndex.Current]
                                exp.Company <- company.Current
                                exp.Position <- position.Current
                                exp.Location <- location.Current
                                exp.Period <- period.Current
                                exp.Description <- description.Current
                                exp.Website <-
                                    if String.IsNullOrWhiteSpace website.Current
                                    then null
                                    else Uri website.Current
                                experiences.Current.[selectedIndex.Current] <- exp
                                experiences.Current |> ignore) ]
                    Button.create [ 
                        Button.content "Delete"
                        StackPanel.classes ["standart"]
                        Button.isEnabled (selectedIndex.Current >= 0)
                        Button.onClick (fun _ ->
                                if selectedIndex.Current >= 0
                                then
                                    experiences.Current.RemoveAt selectedIndex.Current
                                    selectedIndex.Set -1
                                    company.Set ""
                                    position.Set ""
                                    location.Set ""
                                    period.Set ""
                                    description.Set ""
                                    website.Set "") ] ] ] ] ]    

let languagesTabContent (languages: IWritable<ObservableCollection<Language>>, selectedIndex: IWritable<int>,
    name: IWritable<string>, fluency: IWritable<string>, level: IWritable<int>) =
    StackPanel.create [ 
        StackPanel.orientation Orientation.Vertical
        StackPanel.children [ 
            ListBox.create [ 
                ListBox.dataItems languages.Current
                ListBox.selectionMode SelectionMode.Single
                ListBox.onSelectionChanged (fun args ->
                        if args.AddedItems.Count > 0
                        then
                            let lang = args.AddedItems.[0] :?> Language
                            selectedIndex.Set(languages.Current.IndexOf lang)
                            name.Set lang.Name
                            fluency.Set lang.Fluency
                            level.Set lang.Level
                        else
                            selectedIndex.Set -1
                            name.Set ""
                            fluency.Set ""
                            level.Set 0)
                ListBox.itemTemplate (
                    DataTemplateView<_>.create (fun (lang: Language) ->
                            TextBlock.create [ 
                                TextBlock.text (sprintf "%s - %s" lang.Name lang.Fluency) ] ) ) ]
            TextBox.create [ 
                TextBox.watermark "Language name"
                TextBox.text name.Current
                TextBox.onTextChanged (fun t -> name.Set t) ]
            TextBox.create [ 
                TextBox.watermark "Fluency"
                TextBox.text fluency.Current
                TextBox.onTextChanged (fun t -> fluency.Set t) ]
            StackPanel.create [ 
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [ 
                    TextBlock.create [ 
                        TextBlock.text "Level: " ]
                    Slider.create [ 
                        Slider.minimum 0.0
                        Slider.maximum 5.0
                        Slider.tickFrequency 1.0
                        Slider.tickPlacement TickPlacement.Outside
                        Slider.value (float level.Current)
                        Slider.onValueChanged (fun v -> level.Set(int v))
                        Slider.width 200.0 ]
                    TextBlock.create [ 
                        TextBlock.text (string level.Current
                            ) ] ] ]
            StackPanel.create [ 
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Right
                StackPanel.children [ 
                    Button.create [ 
                        Button.content "Add"
                        StackPanel.classes ["standart"]
                        Button.onClick (fun _ ->
                                if not (String.IsNullOrWhiteSpace name.Current)
                                then
                                    let lang =
                                        Language(
                                            name.Current,
                                            fluency.Current,
                                            level.Current
                                        )

                                    languages.Current.Add lang
                                    name.Set ""
                                    fluency.Set ""
                                    level.Set 0) ]
                    Button.create [ 
                        Button.content "Update"
                        StackPanel.classes ["standart"]
                        Button.isEnabled (selectedIndex.Current >= 0)
                        Button.onClick (fun _ ->
                                if selectedIndex.Current >= 0
                                then
                                    let lang = languages.Current.[selectedIndex.Current]
                                    lang.Name <- name.Current
                                    lang.Fluency <- fluency.Current
                                    lang.Level <- level.Current
                                    languages.Current.[selectedIndex.Current] <- lang
                                    languages.Current |> ignore) ]
                    Button.create [ 
                        Button.content "Delete"
                        StackPanel.classes ["standart"]
                        Button.isEnabled (selectedIndex.Current >= 0)
                        Button.onClick (fun _ ->
                                if selectedIndex.Current >= 0
                                then
                                    languages.Current.RemoveAt selectedIndex.Current
                                    selectedIndex.Set -1
                                    name.Set ""
                                    fluency.Set ""
                                    level.Set 0) ] ] ] ] ]    

let skillsTabContent (skills: IWritable<ObservableCollection<Skill>>, selectedIndex: IWritable<int>,
    name: IWritable<string>, keywords: IWritable<string>) =
    StackPanel.create [ 
        StackPanel.orientation Orientation.Vertical
        StackPanel.children [ 
            ListBox.create [ 
                ListBox.dataItems skills.Current
                ListBox.selectionMode SelectionMode.Single
                ListBox.onSelectionChanged (fun args ->
                        if args.AddedItems.Count > 0
                        then
                            let skill = args.AddedItems.[0] :?> Skill
                            selectedIndex.Set(skills.Current.IndexOf skill)
                            name.Set skill.Name
                            keywords.Set(String.Join(", ", skill.Keywords))
                        else
                            selectedIndex.Set -1
                            name.Set ""
                            keywords.Set "")
                ListBox.itemTemplate (
                    DataTemplateView<_>.create
                        (fun (skill: Skill) ->
                            TextBlock.create [ 
                                TextBlock.text skill.Name ] ) ) ]
            TextBox.create [ 
                TextBox.watermark "Skill name"
                TextBox.text name.Current
                TextBox.onTextChanged (fun t -> name.Set t) ]
            TextBox.create [ 
                TextBox.watermark "Keywords (comma separated)"
                TextBox.text keywords.Current
                TextBox.onTextChanged (fun t -> keywords.Set t) ]
            StackPanel.create [ 
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Right
                StackPanel.children [ 
                    Button.create [ 
                        Button.content "Add"
                        StackPanel.classes ["standart"]
                        Button.onClick (fun _ ->
                                if not ( String.IsNullOrWhiteSpace name.Current )
                                then
                                    let keywordsList =
                                        if String.IsNullOrWhiteSpace keywords.Current
                                        then
                                            []
                                        else
                                            keywords.Current.Split([| ',';';' |], 
                                            StringSplitOptions.RemoveEmptyEntries)
                                            |> Array.toList

                                    let skill = Skill(name.Current, keywordsList)
                                    skills.Current.Add skill
                                    name.Set ""
                                    keywords.Set "") ]
                    Button.create [ 
                        Button.content "Update"
                        StackPanel.classes ["standart"]
                        Button.isEnabled (selectedIndex.Current >= 0)
                        Button.onClick (fun _ ->
                                if selectedIndex.Current >= 0
                                then
                                    let skill = skills.Current.[selectedIndex.Current]
                                    skill.Name <- name.Current
                                    skill.Keywords <-
                                        if String.IsNullOrWhiteSpace keywords.Current
                                        then
                                            []
                                        else
                                            keywords.Current.Split([| ',';';' |],
                                            StringSplitOptions.RemoveEmptyEntries)
                                            |> Array.toList

                                    skills.Current.[selectedIndex.Current] <- skill
                                    skills.Current |> ignore) ]
                    Button.create [ 
                        Button.content "Delete"
                        StackPanel.classes ["standart"]
                        Button.isEnabled (selectedIndex.Current >= 0)
                        Button.onClick (fun _ ->
                                if selectedIndex.Current >= 0
                                then
                                    skills.Current.RemoveAt selectedIndex.Current
                                    selectedIndex.Set -1
                                    name.Set ""
                                    keywords.Set "") ] ] ] ] ]   

let cerificationsTabContent (certifications: IWritable<ObservableCollection<Certification>>, selectedIndex: IWritable<int>,
    title: IWritable<string>, issuer: IWritable<string>, date: IWritable<string>,
    label: IWritable<string>, website: IWritable<string>) =
    StackPanel.create [ 
        StackPanel.orientation Orientation.Vertical
        StackPanel.children [ 
            ListBox.create [ 
                ListBox.dataItems certifications.Current
                ListBox.selectionMode SelectionMode.Single
                ListBox.onSelectionChanged (fun args ->
                    if args.AddedItems.Count > 0
                    then
                        let cert = args.AddedItems.[0] :?> Certification
                        selectedIndex.Set(certifications.Current.IndexOf cert)
                        title.Set cert.Title
                        issuer.Set cert.Issuer
                        date.Set cert.Date
                        label.Set cert.Label
                        website.Set(
                            if cert.Website <> null
                            then cert.Website.OriginalString
                            else "")
                    else
                        selectedIndex.Set -1
                        title.Set ""
                        issuer.Set ""
                        date.Set ""
                        label.Set ""
                        website.Set "")
                ListBox.itemTemplate (
                    DataTemplateView<_>.create(fun(cert: Certification) ->
                            TextBlock.create [ 
                                TextBlock.text (sprintf "%s - %s" cert.Title cert.Issuer) ] )
                ) ]
            TextBox.create [ 
                TextBox.watermark "Title"
                TextBox.text title.Current
                TextBox.onTextChanged (fun t -> title.Set t) ]
            TextBox.create [ 
                TextBox.watermark "Issuer"
                TextBox.text issuer.Current
                TextBox.onTextChanged (fun t -> issuer.Set t) ]
            TextBox.create [ 
                TextBox.watermark "Date"
                TextBox.text date.Current
                TextBox.onTextChanged (fun t -> date.Set t) ]
            TextBox.create [ 
                TextBox.watermark "Label"
                TextBox.text label.Current
                TextBox.onTextChanged (fun t -> label.Set t) ]
            TextBox.create [ 
                TextBox.watermark "Website"
                TextBox.text website.Current
                TextBox.onTextChanged (fun t -> website.Set t)
                if website.Current <> "" && not (isValidUrl website.Current)
                then TextBox.classes [ "invalid" ] ]
            StackPanel.create [ 
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Right
                StackPanel.children [ 
                    Button.create [ 
                        Button.content "Add"
                        StackPanel.classes ["standart"]
                        Button.onClick (fun _ ->
                                if not (String.IsNullOrWhiteSpace title.Current)
                                then
                                    let websiteUri =
                                        if String.IsNullOrWhiteSpace website.Current
                                        then null
                                        else Uri website.Current

                                    let cert =
                                        Certification(
                                            title.Current,
                                            issuer.Current,
                                            date.Current,
                                            label.Current,
                                            websiteUri
                                        )

                                    certifications.Current.Add cert
                                    title.Set ""
                                    issuer.Set ""
                                    date.Set ""
                                    label.Set ""
                                    website.Set "") ]
                    Button.create [ 
                        Button.content "Update"
                        StackPanel.classes ["standart"]
                        Button.isEnabled (selectedIndex.Current >= 0)
                        Button.onClick (fun _ ->
                                if selectedIndex.Current >= 0
                                then
                                    let cert = certifications.Current.[selectedIndex.Current]
                                    cert.Title <- title.Current
                                    cert.Issuer <- issuer.Current
                                    cert.Date <- date.Current
                                    cert.Label <- label.Current
                                    cert.Website <-
                                        if String.IsNullOrWhiteSpace website.Current
                                        then null
                                        else Uri website.Current
                                    certifications.Current.[selectedIndex.Current] <- cert
                                    certifications.Current |> ignore) ]
                    Button.create [ 
                        Button.content "Delete"
                        StackPanel.classes ["standart"]
                        Button.isEnabled (selectedIndex.Current >= 0)
                        Button.onClick (fun _ ->
                                if selectedIndex.Current >= 0
                                then
                                    certifications.Current.RemoveAt selectedIndex.Current
                                    selectedIndex.Set -1
                                    title.Set ""
                                    issuer.Set ""
                                    date.Set ""
                                    label.Set ""
                                    website.Set "") ] ] ] ] ]                                        

let educationTabContent (educations: IWritable<ObservableCollection<Education>>, selectedIndex: IWritable<int>,
    school: IWritable<string>, degree: IWritable<string>, area: IWritable<string>,
    grade: IWritable<string>, location: IWritable<string>, period: IWritable<string>,
    website: IWritable<string>) =
    StackPanel.create [ 
        StackPanel.orientation Orientation.Vertical
        StackPanel.children [ 
            ListBox.create [ 
                ListBox.dataItems educations.Current
                ListBox.selectionMode SelectionMode.Single
                ListBox.onSelectionChanged (fun args ->
                    if args.AddedItems.Count > 0
                    then
                        let edu = args.AddedItems.[0] :?> Education
                        selectedIndex.Set(educations.Current.IndexOf edu)
                        school.Set edu.School
                        degree.Set edu.Degree
                        area.Set edu.Area
                        grade.Set edu.Grade
                        location.Set edu.Location
                        period.Set edu.Period
                        website.Set edu.Website
                    else
                        selectedIndex.Set -1
                        school.Set ""
                        degree.Set ""
                        area.Set ""
                        grade.Set ""
                        location.Set ""
                        period.Set ""
                        website.Set "")
                ListBox.itemTemplate ( 
                    DataTemplateView<_>.create(fun (edu: Education) ->
                            TextBlock.create [ 
                                TextBlock.text (sprintf "%s - %s" edu.School edu.Degree) ])
                ) ]
            TextBox.create [ 
                TextBox.watermark "School"
                TextBox.text school.Current
                TextBox.onTextChanged (fun t -> school.Set t) ]
            TextBox.create [ 
                TextBox.watermark "Degree"
                TextBox.text degree.Current
                TextBox.onTextChanged (fun t -> degree.Set t) ]
            TextBox.create [ 
                TextBox.watermark "Area"
                TextBox.text area.Current
                TextBox.onTextChanged (fun t -> area.Set t) ]
            TextBox.create [ 
                TextBox.watermark "Grade"
                TextBox.text grade.Current
                TextBox.onTextChanged (fun t -> grade.Set t) ]
            TextBox.create [ 
                TextBox.watermark "Location"
                TextBox.text location.Current
                TextBox.onTextChanged (fun t -> location.Set t) ]
            TextBox.create [ 
                TextBox.watermark "Period"
                TextBox.text period.Current
                TextBox.onTextChanged (fun t -> period.Set t) ]
            TextBox.create [ 
                TextBox.watermark "Website"
                TextBox.text website.Current
                TextBox.onTextChanged (fun t -> website.Set t) ]
            StackPanel.create [ 
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Right
                StackPanel.children [ 
                    Button.create [ 
                        Button.content "Add"
                        StackPanel.classes ["standart"]
                        Button.onClick (fun _ ->
                                if not (String.IsNullOrWhiteSpace school.Current)
                                then
                                    let edu =
                                        Education(
                                            school.Current,
                                            degree.Current,
                                            area.Current,
                                            grade.Current,
                                            location.Current,
                                            period.Current,
                                            website.Current
                                        )

                                    educations.Current.Add edu
                                    school.Set ""
                                    degree.Set ""
                                    area.Set ""
                                    grade.Set ""
                                    location.Set ""
                                    period.Set ""
                                    website.Set "") ]
                    Button.create [ 
                        Button.content "Update"
                        StackPanel.classes ["standart"]
                        Button.isEnabled (selectedIndex.Current >= 0)
                        Button.onClick (fun _ ->
                                if selectedIndex.Current >= 0
                                then
                                    let edu = educations.Current.[selectedIndex.Current]
                                    edu.School <- school.Current
                                    edu.Degree <- degree.Current
                                    edu.Area <- area.Current
                                    edu.Grade <- grade.Current
                                    edu.Location <- location.Current
                                    edu.Period <- period.Current
                                    edu.Website <- website.Current
                                    educations.Current.[selectedIndex.Current] <- edu
                                    educations.Current |> ignore) ]
                    Button.create [ 
                        Button.content "Delete"
                        StackPanel.classes ["standart"]
                        Button.isEnabled (selectedIndex.Current >= 0)
                        Button.onClick(fun _ ->
                                if selectedIndex.Current >= 0
                                then
                                    educations.Current.RemoveAt selectedIndex.Current
                                    selectedIndex.Set -1
                                    school.Set ""
                                    degree.Set ""
                                    area.Set ""
                                    grade.Set ""
                                    location.Set ""
                                    period.Set ""
                                    website.Set "") ] ] ] ] ]    

[<AbstractClass; Sealed>]
type Views =
    static member main() =
        Component(fun ctx ->
            // states
            let pictureUriState: IWritable<Uri option> = ctx.useState None
            let selectedXsltState: IWritable<XsltFile option> = ctx.useState None
            let xsltFilesState: IWritable<XsltFile array> = ctx.useState Array.empty
            let selectedXsltIndexState = ctx.useState -1
            let nameState = ctx.useState ""
            let headlineState = ctx.useState ""
            let emailState = ctx.useState ""
            let phoneState = ctx.useState ""
            let locationState = ctx.useState ""
            let linksState = ctx.useState (ObservableCollection<string>())
            let summaryState = ctx.useState ""
            let newLinkState = ctx.useState ""
            let selectedLinkState = ctx.useState ""
            let selectedLinkIndexState = ctx.useState -1

            let experiencesState = ctx.useState (ObservableCollection<Experience>())
            let newExpCompanyState = ctx.useState ""
            let newExpPositionState = ctx.useState ""
            let newExpLocationState = ctx.useState ""
            let newExpPeriodState = ctx.useState ""
            let newExpDescriptionState = ctx.useState ""
            let newExpWebsiteState = ctx.useState ""
            let selectedExpIndexState = ctx.useState -1

            let languagesState = ctx.useState (ObservableCollection<Language>())
            let newLangNameState = ctx.useState ""
            let newLangFluencyState = ctx.useState ""
            let newLangLevelState = ctx.useState 0
            let selectedLangIndexState = ctx.useState -1

            let skillsState = ctx.useState (ObservableCollection<Skill>())
            let newSkillNameState = ctx.useState ""
            let newSkillKeywordsState = ctx.useState ""
            let selectedSkillIndexState = ctx.useState -1

            let certificationsState = ctx.useState (ObservableCollection<Certification>())
            let newCertTitleState = ctx.useState ""
            let newCertIssuerState = ctx.useState ""
            let newCertDateState = ctx.useState ""
            let newCertLabelState = ctx.useState ""
            let newCertWebsiteState = ctx.useState ""
            let selectedCertIndexState = ctx.useState -1

            let educationsState = ctx.useState (ObservableCollection<Education>())
            let newEduSchoolState = ctx.useState ""
            let newEduDegreeState = ctx.useState ""
            let newEduAreaState = ctx.useState ""
            let newEduGradeState = ctx.useState ""
            let newEduLocationState = ctx.useState ""
            let newEduPeriodState = ctx.useState ""
            let newEduWebsiteState = ctx.useState ""
            let selectedEduIndexState = ctx.useState -1

            ctx.useEffect (
                handler = (fun _ ->
                    xsltFilesState.Set getXsltFiles

                    if xsltFilesState.Current.Length > 0 then
                        let defaultXslt = xsltFilesState.Current |> Array.head
                        selectedXsltIndexState.Set 0
                        selectedXsltState.Set(Some defaultXslt)),
                triggers = [ EffectTrigger.AfterInit ]
            )

            let imgSource =
                match pictureUriState.Current with
                | None -> null
                | Some uri ->
                    let s = uri.OriginalString

                    if s.StartsWith "data:" then
                        try
                            let comma = s.IndexOf ','

                            if comma >= 0 then
                                let base64 = s.Substring(comma + 1)
                                let bytes = Convert.FromBase64String base64
                                use ms = new MemoryStream(bytes)
                                new Bitmap(ms)
                            else
                                null
                        with _ ->
                            null
                    else
                        let path = HttpUtility.UrlDecode uri.AbsolutePath

                        if not (String.IsNullOrWhiteSpace path) && File.Exists path then
                            new Bitmap(path)
                        else
                            null

            let generateResume (format: GenerateToFormat, outputFilePath: string, xsltFilePath: string option) =
                async {
                    if not (String.IsNullOrWhiteSpace outputFilePath) then
                        let xml =
                            getXmlDoc (
                                pictureUriState.Current
                                |> Option.map (fun u -> u.OriginalString)
                                |> Option.defaultValue "",
                                nameState.Current,
                                headlineState.Current,
                                emailState.Current,
                                phoneState.Current,
                                locationState.Current,
                                linksState.Current,
                                summaryState.Current,
                                experiencesState.Current,
                                languagesState.Current,
                                skillsState.Current,
                                certificationsState.Current,
                                educationsState.Current
                            )

                        match format with
                        | GenerateToFormat.Xml -> xml.Save outputFilePath
                        | GenerateToFormat.Html ->
                            match xsltFilePath with
                            | Some xsltPath ->
                                let htmlOpt = transformXmlToHtml (xml, xsltPath)
                                match htmlOpt with
                                | Some html -> File.WriteAllText(outputFilePath, html)
                                |None -> ()
                            | None -> ()
                        | GenerateToFormat.Pdf ->
                            match xsltFilePath with
                            | Some xsltPath ->
                                let htmlOpt = transformXmlToHtml (xml, xsltPath)
                                match htmlOpt with
                                | Some html -> do! generatePdfFromHtml (html, outputFilePath) |> Async.AwaitTask
                                |None -> ()                                                               
                            | None -> ()
                }
                |> Async.StartImmediate

            let startGenerateResume (format: GenerateToFormat) =
                async {
                    let! file = chooseFile format

                    match file with
                    | None -> ()
                    | Some outputPath ->
                        match format with
                        | Xml -> generateResume (format, outputPath, None)
                        | _ ->
                            match selectedXsltState.Current with
                            | Some xsltFile -> generateResume (format, outputPath, Some xsltFile.Path)
                            | None -> ()
                }
                |> Async.StartImmediate

            let states =
                { pictureUriState = pictureUriState
                  nameState = nameState
                  headlineState = headlineState
                  emailState = emailState
                  phoneState = phoneState
                  locationState = locationState
                  linksState = linksState
                  summaryState = summaryState
                  experiencesState = experiencesState
                  languagesState = languagesState
                  skillsState = skillsState
                  certificationsState = certificationsState
                  educationsState = educationsState }
            
            let getTabHeader(title: string, symbol: Symbol) =
                StackPanel.create [
                    StackPanel.orientation Orientation.Horizontal
                    StackPanel.spacing 8.0
                    StackPanel.children [
                        SymbolIcon.create [
                            SymbolIcon.symbol symbol
                            SymbolIcon.width 20.0
                            SymbolIcon.height 20.0
                            SymbolIcon.verticalAlignment VerticalAlignment.Center
                        ]
                        TextBlock.create [ 
                            TextBlock.verticalAlignment VerticalAlignment.Center
                            TextBlock.text title ]
                    ]]


            DockPanel.create
                [ DockPanel.children
                      [ Grid.create
                            [ Grid.dock Dock.Top
                              Grid.margin 4
                              Grid.columnDefinitions "*"
                              Grid.rowDefinitions "*, Auto"
                              Grid.children
                                  [ ScrollViewer.create
                                        [ Grid.row 0
                                          ScrollViewer.content (
                                            TabControl.create
                                                [
                                                TabControl.tabStripPlacement Dock.Left
                                                TabControl.viewItems [
                                                    TabItem.create [
                                                            TabItem.header (getTabHeader("Picture", Symbol.Image))
                                                            TabItem.content (pictureTabConent(imgSource, pictureUriState))]
                                                    TabItem.create [
                                                            TabItem.header (getTabHeader("Basic Info", Symbol.PersonInfo))
                                                            TabItem.content (basicInfoTabContent(nameState, headlineState, 
                                                                emailState, phoneState, locationState, linksState,
                                                                selectedLinkState, newLinkState, selectedLinkIndexState))]
                                                    TabItem.create [
                                                            TabItem.header (getTabHeader("Summary", Symbol.Markdown))
                                                            TabItem.content (summaryTabContent summaryState)]
                                                    TabItem.create [
                                                            TabItem.header (getTabHeader("Experience", Symbol.Briefcase))
                                                            TabItem.content (experienceTabContent(experiencesState, selectedExpIndexState, 
                                                                newExpCompanyState, newExpPositionState, newExpLocationState, newExpPeriodState, 
                                                                newExpDescriptionState,newExpWebsiteState))]
                                                    TabItem.create [
                                                            TabItem.header (getTabHeader("Languages", Symbol.Globe))
                                                            TabItem.content (languagesTabContent(languagesState, selectedLangIndexState, 
                                                                newLangNameState, newLangFluencyState, newLangLevelState))]
                                                    TabItem.create [
                                                            TabItem.header (getTabHeader("Skills", Symbol.Star))
                                                            TabItem.content (skillsTabContent(skillsState, selectedSkillIndexState, 
                                                                newSkillNameState, newSkillKeywordsState))]
                                                    TabItem.create [
                                                            TabItem.header (getTabHeader("Cerifications", Symbol.Certificate))
                                                            TabItem.content (cerificationsTabContent(certificationsState, selectedCertIndexState, 
                                                                newCertTitleState, newCertIssuerState, newCertDateState, newCertLabelState, newCertWebsiteState))]
                                                    TabItem.create [
                                                            TabItem.header (getTabHeader("Education", Symbol.Book))
                                                            TabItem.content (educationTabContent(educationsState, selectedEduIndexState, newEduSchoolState, 
                                                                newEduDegreeState, newEduAreaState, newEduGradeState, newEduLocationState, newEduPeriodState, newEduWebsiteState))]                                                                                                                                                                                                                                                                                                                                                                                                                                                            
                                                ] ] ) ]

                                    // bottom controls
                                    Border.create [
                                        Grid.row 1
                                        Border.borderBrush "Gray"
                                        Border.borderThickness(0.0, 1.0, 0.0, 0.0)
                                        Border.child (
                                            StackPanel.create [
                                                StackPanel.orientation  Orientation.Horizontal
                                                StackPanel.orientation Orientation.Horizontal
                                                StackPanel.horizontalAlignment HorizontalAlignment.Right
                                                StackPanel.verticalAlignment VerticalAlignment.Bottom
                                                StackPanel.children [
                                                    ComboBox.create [
                                                        ComboBox.dataItems xsltFilesState.Current
                                                        ComboBox.selectedIndex selectedXsltIndexState.Current
                                                        ComboBox.onSelectedIndexChanged (fun index ->
                                                            selectedXsltIndexState.Set index
                                                            index |> xsltFilesState.Current.GetValue :?> XsltFile
                                                            |> Some
                                                            |> selectedXsltState.Set)
                                                        ComboBox.itemTemplate (
                                                            DataTemplateView<_>.create (fun (f: XsltFile) ->
                                                                TextBlock.create [ TextBlock.text f.Name ])
                                                        )                                                                                                                 
                                                    ]
                                                    Button.create [
                                                        Button.content "Load from XML"
                                                        Button.onClick (fun _ -> loadFromXml states)
                                                    ]
                                                    DropDownButton.create [
                                                        DropDownButton.content "Save As"
                                                        DropDownButton.flyout (
                                                          MenuFlyout.create [ 
                                                            MenuFlyout.placement PlacementMode.BottomEdgeAlignedRight
                                                            MenuFlyout.viewItems [
                                                                MenuItem.create [ 
                                                                    MenuItem.header "XML"
                                                                    MenuItem.onClick (fun _ -> startGenerateResume Xml) ]
                                                                MenuItem.create [ 
                                                                    MenuItem.header "HTML"
                                                                    MenuItem.isEnabled selectedXsltState.Current.IsSome
                                                                    MenuItem.onClick (fun _ -> startGenerateResume Html) ]
                                                                MenuItem.create [ 
                                                                    MenuItem.header "PDF"
                                                                    MenuItem.isEnabled selectedXsltState.Current.IsSome
                                                                    MenuItem.onClick (fun _ -> startGenerateResume Pdf) ] 
                                                            ] ]                                                            
                                                        )
                                                    ]
                                                ]                                              
                                            ]                                            
                                        )]
                                    ]
                                ] ] ] )

type MainWindow() as this =
    inherit HostWindow()

    do
        let invalidTextBoxStyle =
            let style = Style(fun x -> x.OfType<TextBox>().Class "invalid")
            style.Setters.Add(Setter(TextBox.BorderBrushProperty, Brushes.Red))
            style :> IStyle    
            
        let tabContentStyle =
            let style = Style(fun x -> x.OfType<StackPanel>())
            style.Setters.Add(Setter(StackPanel.SpacingProperty, 4.0))
            style.Setters.Add(Setter(StackPanel.MarginProperty, Thickness.Parse "10.0"))
            style :> IStyle  
            
        let buttonStyle =
            let style = Style(fun x -> x.OfType<Button>().Class "standart")
            style.Setters.Add(Setter(Button.WidthProperty, 100.0))
            style.Setters.Add(Setter(Button.HorizontalContentAlignmentProperty, HorizontalAlignment.Center))
            style :> IStyle              

        base.Title <- "Resume Generator"
        base.Width <- 800.0
        base.Height <- 600.0
#if INTERACTIVE        
        base.Icon <- new WindowIcon(new Bitmap(Path.Combine(getCurrentDirectory, "favicon.ico")))
#endif        
        base.Styles.Add invalidTextBoxStyle
        base.Styles.Add tabContentStyle
        base.Styles.Add buttonStyle
        this.Content <- Views.main ()

type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Add(Themes.Fluent.FluentTheme())
        this.RequestedThemeVariant <- ThemeVariant.Dark
        this.Styles.Load "avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml"

    override this.OnFrameworkInitializationCompleted() =

        match this.ApplicationLifetime with
        | :? ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime as desktop ->
            desktop.MainWindow <- MainWindow()
        | _ -> ()


#if INTERACTIVE
let app =
    AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .StartWithClassicDesktopLifetime 
        [||]
#endif
#if COMPILED
module Program =
    [<STAThread>]
    [<EntryPoint>]
    let main (args: string[]) =
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .StartWithClassicDesktopLifetime
            args
#endif    
