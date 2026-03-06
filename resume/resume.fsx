#r "nuget: Avalonia"
#r "nuget: Avalonia.Desktop"
#r "nuget: Avalonia.Themes.Fluent"
#r "nuget: Avalonia.FuncUI"
#r "nuget: PuppeteerSharp"

open System
open System.Collections.ObjectModel
open System.IO
open System.Text.RegularExpressions
open System.Xml.Linq
open System.Xml.Xsl
open System.Web
open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Hosts
open Avalonia.Layout
open Avalonia.Media.Imaging
open Avalonia.Platform.Storage
open PuppeteerSharp
open PuppeteerSharp.Media
open Avalonia.Controls.Primitives
open Avalonia.Styling
open Avalonia.Media

type GenerateToFormat =
    | Pdf
    | Xml
    | Html

type XsltFile (name: string, path: string) =
    member val Name = name with get, set
    member val Path = path with get, set

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

let emailRegex = Regex("^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)
let phoneRegex = Regex("^\+?[0-9\s\-()]+$", RegexOptions.Compiled)

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
    let folder = Path.Combine(__SOURCE_DIRECTORY__, "xslt") |> DirectoryInfo
    if folder.Exists then  
        folder.GetFiles "*.xslt" |> Array.map(fun f -> XsltFile(Path.GetFileNameWithoutExtension f.Name, f.FullName))
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

let getXmlDoc
    (
        picturePath: string,
        name: string,
        headline: string,
        email: string,
        phone: string,
        location: string,
        links: ObservableCollection<string>,
        summary: string,
        experiences: ObservableCollection<Experience>,
        languages: ObservableCollection<Language>,
        skills: ObservableCollection<Skill>,
        certifications: ObservableCollection<Certification>,
        educations: ObservableCollection<Education>
    ) =
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

let transformXmlToHtml (doc: XDocument, xsltPath:string) =
    //let xsltPath = Path.Combine(__SOURCE_DIRECTORY__, "xslt", "default.xslt")
    let xslt = new XslCompiledTransform()
    xslt.Load xsltPath
    use stringWriter = new StringWriter()
    use docReader = doc.CreateReader()
    xslt.Transform(docReader, null, stringWriter)
    stringWriter.ToString()

let generatePdfFromHtml (htmlContent: string, outputPath: string) =
    task {
        let browserFetcher = new BrowserFetcher()
        let! _installedBrowser = browserFetcher.DownloadAsync()
        use! browser = Puppeteer.LaunchAsync(LaunchOptions(Headless = true))
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
                    states.pictureUriState.Set(None)

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
                            |> fun e -> if isNull e then 0 else Int32.Parse(e.Value)

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
                printfn "Failed to load XML '%s': %s" path ex.Message
        | None -> ()
    }
    |> Async.StartImmediate

[<AbstractClass; Sealed>]
type Views =
    static member main() =
        Component(fun ctx ->
            // states
            let pictureUriState: IWritable<Uri option> = ctx.useState None
            let selectedXsltState: IWritable<XsltFile option> = ctx.useState None
            let xsltFilesState : IWritable<XsltFile array> = ctx.useState Array.empty
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
                handler = 
                    (fun _ ->
                        xsltFilesState.Set getXsltFiles
                        if xsltFilesState.Current.Length > 0 
                        then 
                            let defaultXslt = xsltFilesState.Current |> Array.head
                            selectedXsltIndexState.Set 0
                            selectedXsltState.Set (Some defaultXslt)
                ), triggers = [ EffectTrigger.AfterInit ])


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
                                let html = transformXmlToHtml (xml, xsltPath)
                                File.WriteAllText(outputFilePath, html)
                            | None -> ()
                        | GenerateToFormat.Pdf ->
                            match xsltFilePath with
                            | Some xsltPath ->
                                let html = transformXmlToHtml (xml, xsltPath)
                                do! generatePdfFromHtml (html, outputFilePath) |> Async.AwaitTask
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
                            | None ->()
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
                                              StackPanel.create
                                                  [ StackPanel.orientation Orientation.Vertical
                                                    StackPanel.spacing 4.0
                                                    StackPanel.children
                                                        [ // Picture Expander
                                                          Expander.create
                                                              [ Expander.header "Picture"
                                                                Expander.horizontalAlignment
                                                                    HorizontalAlignment.Stretch
                                                                Expander.content (
                                                                    StackPanel.create
                                                                        [ StackPanel.orientation Orientation.Horizontal
                                                                          StackPanel.verticalAlignment
                                                                              VerticalAlignment.Center
                                                                          StackPanel.spacing 2.0
                                                                          StackPanel.children
                                                                              [ Image.create
                                                                                    [ Image.source imgSource
                                                                                      Image.maxHeight 90.0
                                                                                      Image.maxWidth 90.0 ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "Image path"
                                                                                      TextBox.width 300.0
                                                                                      TextBox.height 30.0
                                                                                      TextBox.text (
                                                                                          pictureUriState.Current
                                                                                          |> Option.map (fun u ->
                                                                                              u.OriginalString)
                                                                                          |> Option.defaultValue ""
                                                                                      )
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          match tryMakeUri t with
                                                                                          | Some u ->
                                                                                              pictureUriState.Set(
                                                                                                  Some u
                                                                                              )
                                                                                          | None ->
                                                                                              pictureUriState.Set None) ]
                                                                                Button.create
                                                                                    [ Button.content "..."
                                                                                      Button.tip "Choose picture"
                                                                                      Button.onClick (fun _ ->
                                                                                          async {
                                                                                              let! opt =
                                                                                                  choosePicturePath ()

                                                                                              opt
                                                                                              |> Option.iter
                                                                                                  (fun p ->
                                                                                                      pictureUriState
                                                                                                          .Set(
                                                                                                              Some p
                                                                                                          ))
                                                                                          }
                                                                                          |> Async.StartImmediate) ] ] ]
                                                                ) ]

                                                          // Basic Information Expander
                                                          Expander.create
                                                              [ Expander.header "Basic Information"
                                                                Expander.horizontalAlignment
                                                                    HorizontalAlignment.Stretch
                                                                Expander.content (
                                                                    StackPanel.create
                                                                        [ StackPanel.orientation Orientation.Vertical
                                                                          StackPanel.spacing 2.0
                                                                          StackPanel.children
                                                                              [ TextBox.create
                                                                                    [ TextBox.watermark "Name"
                                                                                      TextBox.text nameState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          nameState.Set t) ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "Headline"
                                                                                      TextBox.text
                                                                                          headlineState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          headlineState.Set t) ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "Email"
                                                                                      TextBox.text emailState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          emailState.Set t)
                                                                                      if
                                                                                          emailState.Current <> ""
                                                                                          && not (
                                                                                              emailRegex.IsMatch
                                                                                                  emailState.Current
                                                                                          )
                                                                                      then
                                                                                          TextBox.classes [ "invalid" ] ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "Phone"
                                                                                      TextBox.text phoneState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          phoneState.Set t)
                                                                                      if
                                                                                          phoneState.Current <> ""
                                                                                          && not (
                                                                                              phoneRegex.IsMatch
                                                                                                  phoneState.Current
                                                                                          )
                                                                                      then
                                                                                          TextBox.classes [ "invalid" ] ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "Location"
                                                                                      TextBox.text
                                                                                          locationState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          locationState.Set t) ]
                                                                                TextBlock.create
                                                                                    [ TextBlock.text "Links:" ]
                                                                                ListBox.create
                                                                                    [ ListBox.dataItems
                                                                                          linksState.Current
                                                                                      ListBox.selectionMode
                                                                                          SelectionMode.Single
                                                                                      ListBox.selectedItem
                                                                                          selectedLinkState.Current
                                                                                      ListBox.onSelectedIndexChanged
                                                                                          (fun index ->
                                                                                              selectedLinkIndexState.Set
                                                                                                  index)
                                                                                      ListBox.onSelectedItemChanged
                                                                                          (fun item ->
                                                                                              newLinkState.Set(
                                                                                                  item |> string
                                                                                              )

                                                                                              selectedLinkState.Set(
                                                                                                  item |> string
                                                                                              )) ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "New link"
                                                                                      TextBox.horizontalAlignment
                                                                                          HorizontalAlignment.Stretch
                                                                                      TextBox.text newLinkState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          newLinkState.Set t)
                                                                                      if
                                                                                          newLinkState.Current <> ""
                                                                                          && not (
                                                                                              isValidUrl
                                                                                                  newLinkState.Current
                                                                                          )
                                                                                      then
                                                                                          TextBox.classes [ "invalid" ] ]
                                                                                StackPanel.create
                                                                                    [ StackPanel.horizontalAlignment
                                                                                          HorizontalAlignment.Right
                                                                                      StackPanel.orientation
                                                                                          Orientation.Horizontal
                                                                                      StackPanel.spacing 4.0
                                                                                      StackPanel.children
                                                                                          [ Button.create
                                                                                                [ Button.content "Add"
                                                                                                  Button.onClick
                                                                                                      (fun _ ->
                                                                                                          if
                                                                                                              not (
                                                                                                                  String.IsNullOrWhiteSpace
                                                                                                                      newLinkState.Current
                                                                                                              )
                                                                                                              && isValidUrl
                                                                                                                  newLinkState.Current
                                                                                                              && linksState.Current.Contains
                                                                                                                  newLinkState.Current
                                                                                                                 |> not
                                                                                                          then
                                                                                                              linksState.Current.Add
                                                                                                                  newLinkState.Current

                                                                                                              newLinkState.Set
                                                                                                                  "") ]
                                                                                            Button.create
                                                                                                [ Button.content
                                                                                                      "Update"
                                                                                                  Button.isEnabled (
                                                                                                      not (
                                                                                                          String.IsNullOrEmpty
                                                                                                              selectedLinkState.Current
                                                                                                      )
                                                                                                  )
                                                                                                  Button.onClick
                                                                                                      (fun _ ->
                                                                                                          if
                                                                                                              linksState.Current.Count > 0
                                                                                                              && selectedLinkIndexState.Current
                                                                                                                 >= 0
                                                                                                          then
                                                                                                              linksState.Current.[selectedLinkIndexState.Current] <-
                                                                                                                  newLinkState.Current) ]
                                                                                            Button.create
                                                                                                [ Button.content
                                                                                                      "Delete"
                                                                                                  Button.isEnabled (
                                                                                                      not (
                                                                                                          String.IsNullOrEmpty
                                                                                                              selectedLinkState.Current
                                                                                                      )
                                                                                                  )
                                                                                                  Button.onClick
                                                                                                      (fun _ ->
                                                                                                          if
                                                                                                              linksState.Current.Contains
                                                                                                                  selectedLinkState.Current
                                                                                                          then
                                                                                                              if
                                                                                                                  linksState.Current.Remove
                                                                                                                      selectedLinkState.Current
                                                                                                              then
                                                                                                                  selectedLinkState.Set
                                                                                                                      "") ] ] ]

                                                                                ] ]
                                                                ) ]

                                                          // Summary Expander
                                                          Expander.create
                                                              [ Expander.header "Summary"
                                                                Expander.horizontalAlignment
                                                                    HorizontalAlignment.Stretch
                                                                Expander.content (
                                                                    TextBox.create
                                                                        [ TextBox.watermark "HTML summary"
                                                                          TextBox.text summaryState.Current
                                                                          TextBox.onTextChanged (fun t ->
                                                                              summaryState.Set t)
                                                                          TextBox.acceptsReturn true
                                                                          TextBox.height 150.0
                                                                          TextBox.verticalScrollBarVisibility
                                                                              ScrollBarVisibility.Auto
                                                                          TextBox.horizontalScrollBarVisibility
                                                                              ScrollBarVisibility.Auto ]
                                                                ) ]

                                                          // Experiences Expander
                                                          Expander.create
                                                              [ Expander.header "Experience"
                                                                Expander.horizontalAlignment
                                                                    HorizontalAlignment.Stretch
                                                                Expander.isExpanded false
                                                                Expander.content (
                                                                    StackPanel.create
                                                                        [ StackPanel.orientation Orientation.Vertical
                                                                          StackPanel.spacing 2.0
                                                                          StackPanel.children
                                                                              [ ListBox.create
                                                                                    [ ListBox.dataItems
                                                                                          experiencesState.Current
                                                                                      ListBox.selectionMode
                                                                                          SelectionMode.Single
                                                                                      ListBox.onSelectionChanged
                                                                                          (fun args ->
                                                                                              if
                                                                                                  args.AddedItems.Count > 0
                                                                                              then
                                                                                                  let exp =
                                                                                                      args.AddedItems.[0]
                                                                                                      :?> Experience

                                                                                                  selectedExpIndexState
                                                                                                      .Set(
                                                                                                          experiencesState
                                                                                                              .Current
                                                                                                              .IndexOf(
                                                                                                                  exp
                                                                                                              )
                                                                                                      )

                                                                                                  newExpCompanyState
                                                                                                      .Set(
                                                                                                          exp.Company
                                                                                                      )

                                                                                                  newExpPositionState
                                                                                                      .Set(
                                                                                                          exp.Position
                                                                                                      )

                                                                                                  newExpLocationState
                                                                                                      .Set(
                                                                                                          exp.Location
                                                                                                      )

                                                                                                  newExpPeriodState
                                                                                                      .Set(exp.Period)

                                                                                                  newExpDescriptionState
                                                                                                      .Set(
                                                                                                          exp.Description
                                                                                                      )

                                                                                                  newExpWebsiteState
                                                                                                      .Set(
                                                                                                          if
                                                                                                              exp.Website
                                                                                                              <> null
                                                                                                          then
                                                                                                              exp.Website.OriginalString
                                                                                                          else
                                                                                                              ""
                                                                                                      )
                                                                                              else
                                                                                                  selectedExpIndexState
                                                                                                      .Set(-1)

                                                                                                  newExpCompanyState
                                                                                                      .Set("")

                                                                                                  newExpPositionState
                                                                                                      .Set("")

                                                                                                  newExpLocationState
                                                                                                      .Set("")

                                                                                                  newExpPeriodState
                                                                                                      .Set("")

                                                                                                  newExpDescriptionState
                                                                                                      .Set("")

                                                                                                  newExpWebsiteState
                                                                                                      .Set(""))
                                                                                      ListBox.itemTemplate (
                                                                                          DataTemplateView<_>.create
                                                                                              (fun (exp: Experience) ->
                                                                                                  TextBlock.create
                                                                                                      [ TextBlock.text (
                                                                                                            sprintf
                                                                                                                "%s - %s"
                                                                                                                exp.Company
                                                                                                                exp.Position
                                                                                                        ) ])
                                                                                      ) ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "Company"
                                                                                      TextBox.text
                                                                                          newExpCompanyState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          newExpCompanyState.Set t) ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "Position"
                                                                                      TextBox.text
                                                                                          newExpPositionState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          newExpPositionState.Set t) ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "Location"
                                                                                      TextBox.text
                                                                                          newExpLocationState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          newExpLocationState.Set t) ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "Period"
                                                                                      TextBox.text
                                                                                          newExpPeriodState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          newExpPeriodState.Set t) ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "Website"
                                                                                      TextBox.text
                                                                                          newExpWebsiteState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          newExpWebsiteState.Set t)
                                                                                      if
                                                                                          newExpWebsiteState.Current
                                                                                          <> ""
                                                                                          && not (
                                                                                              isValidUrl
                                                                                                  newExpWebsiteState.Current
                                                                                          )
                                                                                      then
                                                                                          TextBox.classes [ "invalid" ] ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "Description"
                                                                                      TextBox.text
                                                                                          newExpDescriptionState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          newExpDescriptionState.Set t)
                                                                                      TextBox.acceptsReturn true
                                                                                      TextBox.height 80.0
                                                                                      TextBox.verticalScrollBarVisibility
                                                                                          ScrollBarVisibility.Auto ]
                                                                                StackPanel.create
                                                                                    [ StackPanel.orientation
                                                                                          Orientation.Horizontal
                                                                                      StackPanel.horizontalAlignment
                                                                                          HorizontalAlignment.Right
                                                                                      StackPanel.spacing 4.0
                                                                                      StackPanel.children
                                                                                          [ Button.create
                                                                                                [ Button.content "Add"
                                                                                                  Button.onClick
                                                                                                      (fun _ ->
                                                                                                          if
                                                                                                              not (
                                                                                                                  String.IsNullOrWhiteSpace
                                                                                                                      newExpCompanyState.Current
                                                                                                              )
                                                                                                          then
                                                                                                              let website =
                                                                                                                  if
                                                                                                                      String.IsNullOrWhiteSpace
                                                                                                                          newExpWebsiteState.Current
                                                                                                                  then
                                                                                                                      null
                                                                                                                  else
                                                                                                                      Uri(
                                                                                                                          newExpWebsiteState.Current
                                                                                                                      )

                                                                                                              let exp =
                                                                                                                  Experience(
                                                                                                                      newExpCompanyState.Current,
                                                                                                                      website,
                                                                                                                      newExpPositionState.Current,
                                                                                                                      newExpLocationState.Current,
                                                                                                                      newExpPeriodState.Current,
                                                                                                                      newExpDescriptionState.Current
                                                                                                                  )

                                                                                                              experiencesState
                                                                                                                  .Current
                                                                                                                  .Add(
                                                                                                                      exp
                                                                                                                  )

                                                                                                              newExpCompanyState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newExpPositionState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newExpLocationState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newExpPeriodState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newExpDescriptionState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newExpWebsiteState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )) ]
                                                                                            Button.create
                                                                                                [ Button.content
                                                                                                      "Update"
                                                                                                  Button.isEnabled (
                                                                                                      selectedExpIndexState.Current
                                                                                                      >= 0
                                                                                                  )
                                                                                                  Button.onClick
                                                                                                      (fun _ ->
                                                                                                          if
                                                                                                              selectedExpIndexState.Current
                                                                                                              >= 0
                                                                                                          then
                                                                                                              let exp =
                                                                                                                  experiencesState.Current.[selectedExpIndexState.Current]

                                                                                                              exp.Company <-
                                                                                                                  newExpCompanyState.Current

                                                                                                              exp.Position <-
                                                                                                                  newExpPositionState.Current

                                                                                                              exp.Location <-
                                                                                                                  newExpLocationState.Current

                                                                                                              exp.Period <-
                                                                                                                  newExpPeriodState.Current

                                                                                                              exp.Description <-
                                                                                                                  newExpDescriptionState.Current

                                                                                                              exp.Website <-
                                                                                                                  if
                                                                                                                      String.IsNullOrWhiteSpace
                                                                                                                          newExpWebsiteState.Current
                                                                                                                  then
                                                                                                                      null
                                                                                                                  else
                                                                                                                      Uri
                                                                                                                          newExpWebsiteState.Current

                                                                                                              experiencesState.Current.[selectedExpIndexState.Current] <-
                                                                                                                  exp

                                                                                                              experiencesState.Current
                                                                                                              |> ignore) ]
                                                                                            Button.create
                                                                                                [ Button.content
                                                                                                      "Delete"
                                                                                                  Button.isEnabled (
                                                                                                      selectedExpIndexState.Current
                                                                                                      >= 0
                                                                                                  )
                                                                                                  Button.onClick
                                                                                                      (fun _ ->
                                                                                                          if
                                                                                                              selectedExpIndexState.Current
                                                                                                              >= 0
                                                                                                          then
                                                                                                              experiencesState
                                                                                                                  .Current
                                                                                                                  .RemoveAt(
                                                                                                                      selectedExpIndexState.Current
                                                                                                                  )

                                                                                                              selectedExpIndexState
                                                                                                                  .Set(
                                                                                                                      -1
                                                                                                                  )

                                                                                                              newExpCompanyState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newExpPositionState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newExpLocationState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newExpPeriodState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newExpDescriptionState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newExpWebsiteState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )) ] ] ] ] ]
                                                                ) ]

                                                          // Languages Expander
                                                          Expander.create
                                                              [ Expander.header "Languages"
                                                                Expander.horizontalAlignment
                                                                    HorizontalAlignment.Stretch
                                                                Expander.isExpanded false
                                                                Expander.content (
                                                                    StackPanel.create
                                                                        [ StackPanel.orientation Orientation.Vertical
                                                                          StackPanel.spacing 2.0
                                                                          StackPanel.children
                                                                              [ ListBox.create
                                                                                    [ ListBox.dataItems
                                                                                          languagesState.Current
                                                                                      ListBox.selectionMode
                                                                                          SelectionMode.Single
                                                                                      ListBox.onSelectionChanged
                                                                                          (fun args ->
                                                                                              if
                                                                                                  args.AddedItems.Count > 0
                                                                                              then
                                                                                                  let lang =
                                                                                                      args.AddedItems.[0]
                                                                                                      :?> Language

                                                                                                  selectedLangIndexState
                                                                                                      .Set(
                                                                                                          languagesState
                                                                                                              .Current
                                                                                                              .IndexOf(
                                                                                                                  lang
                                                                                                              )
                                                                                                      )

                                                                                                  newLangNameState.Set(
                                                                                                      lang.Name
                                                                                                  )

                                                                                                  newLangFluencyState
                                                                                                      .Set(
                                                                                                          lang.Fluency
                                                                                                      )

                                                                                                  newLangLevelState
                                                                                                      .Set(lang.Level)
                                                                                              else
                                                                                                  selectedLangIndexState
                                                                                                      .Set(-1)

                                                                                                  newLangNameState.Set(
                                                                                                      ""
                                                                                                  )

                                                                                                  newLangFluencyState
                                                                                                      .Set("")

                                                                                                  newLangLevelState
                                                                                                      .Set(0))
                                                                                      ListBox.itemTemplate (
                                                                                          DataTemplateView<_>.create
                                                                                              (fun (lang: Language) ->
                                                                                                  TextBlock.create
                                                                                                      [ TextBlock.text (
                                                                                                            sprintf
                                                                                                                "%s - %s"
                                                                                                                lang.Name
                                                                                                                lang.Fluency
                                                                                                        ) ])
                                                                                      ) ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "Language name"
                                                                                      TextBox.text
                                                                                          newLangNameState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          newLangNameState.Set t) ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "Fluency"
                                                                                      TextBox.text
                                                                                          newLangFluencyState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          newLangFluencyState.Set t) ]
                                                                                StackPanel.create
                                                                                    [ StackPanel.orientation
                                                                                          Orientation.Horizontal
                                                                                      StackPanel.children
                                                                                          [ TextBlock.create
                                                                                                [ TextBlock.text
                                                                                                      "Level: " ]
                                                                                            Slider.create
                                                                                                [ Slider.minimum 0.0
                                                                                                  Slider.maximum 5.0
                                                                                                  Slider.tickFrequency
                                                                                                      1.0
                                                                                                  Slider.tickPlacement
                                                                                                      TickPlacement.Outside
                                                                                                  Slider.value (
                                                                                                      float
                                                                                                          newLangLevelState.Current
                                                                                                  )
                                                                                                  Slider.onValueChanged
                                                                                                      (fun v ->
                                                                                                          newLangLevelState
                                                                                                              .Set(
                                                                                                                  int
                                                                                                                      v
                                                                                                              ))
                                                                                                  Slider.width 200.0 ]
                                                                                            TextBlock.create
                                                                                                [ TextBlock.text (
                                                                                                      string
                                                                                                          newLangLevelState.Current
                                                                                                  ) ] ] ]
                                                                                StackPanel.create
                                                                                    [ StackPanel.orientation
                                                                                          Orientation.Horizontal
                                                                                      StackPanel.horizontalAlignment
                                                                                          HorizontalAlignment.Right
                                                                                      StackPanel.spacing 4.0
                                                                                      StackPanel.children
                                                                                          [ Button.create
                                                                                                [ Button.content "Add"
                                                                                                  Button.onClick
                                                                                                      (fun _ ->
                                                                                                          if
                                                                                                              not (
                                                                                                                  String.IsNullOrWhiteSpace
                                                                                                                      newLangNameState.Current
                                                                                                              )
                                                                                                          then
                                                                                                              let lang =
                                                                                                                  Language(
                                                                                                                      newLangNameState.Current,
                                                                                                                      newLangFluencyState.Current,
                                                                                                                      newLangLevelState.Current
                                                                                                                  )

                                                                                                              languagesState
                                                                                                                  .Current
                                                                                                                  .Add(
                                                                                                                      lang
                                                                                                                  )

                                                                                                              newLangNameState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newLangFluencyState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newLangLevelState
                                                                                                                  .Set(
                                                                                                                      0
                                                                                                                  )) ]
                                                                                            Button.create
                                                                                                [ Button.content
                                                                                                      "Update"
                                                                                                  Button.isEnabled (
                                                                                                      selectedLangIndexState.Current
                                                                                                      >= 0
                                                                                                  )
                                                                                                  Button.onClick
                                                                                                      (fun _ ->
                                                                                                          if
                                                                                                              selectedLangIndexState.Current
                                                                                                              >= 0
                                                                                                          then
                                                                                                              let lang =
                                                                                                                  languagesState.Current.[selectedLangIndexState.Current]

                                                                                                              lang.Name <-
                                                                                                                  newLangNameState.Current

                                                                                                              lang.Fluency <-
                                                                                                                  newLangFluencyState.Current

                                                                                                              lang.Level <-
                                                                                                                  newLangLevelState.Current

                                                                                                              languagesState.Current.[selectedLangIndexState.Current] <-
                                                                                                                  lang

                                                                                                              languagesState.Current
                                                                                                              |> ignore) ]
                                                                                            Button.create
                                                                                                [ Button.content
                                                                                                      "Delete"
                                                                                                  Button.isEnabled (
                                                                                                      selectedLangIndexState.Current
                                                                                                      >= 0
                                                                                                  )
                                                                                                  Button.onClick
                                                                                                      (fun _ ->
                                                                                                          if
                                                                                                              selectedLangIndexState.Current
                                                                                                              >= 0
                                                                                                          then
                                                                                                              languagesState
                                                                                                                  .Current
                                                                                                                  .RemoveAt(
                                                                                                                      selectedLangIndexState.Current
                                                                                                                  )

                                                                                                              selectedLangIndexState
                                                                                                                  .Set(
                                                                                                                      -1
                                                                                                                  )

                                                                                                              newLangNameState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newLangFluencyState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newLangLevelState
                                                                                                                  .Set(
                                                                                                                      0
                                                                                                                  )) ] ] ] ] ]
                                                                ) ]

                                                          // Skills Expander
                                                          Expander.create
                                                              [ Expander.header "Skills"
                                                                Expander.horizontalAlignment
                                                                    HorizontalAlignment.Stretch
                                                                Expander.isExpanded false
                                                                Expander.content (
                                                                    StackPanel.create
                                                                        [ StackPanel.orientation Orientation.Vertical
                                                                          StackPanel.spacing 2.0
                                                                          StackPanel.children
                                                                              [ ListBox.create
                                                                                    [ ListBox.dataItems
                                                                                          skillsState.Current
                                                                                      ListBox.selectionMode
                                                                                          SelectionMode.Single
                                                                                      ListBox.onSelectionChanged
                                                                                          (fun args ->
                                                                                              if
                                                                                                  args.AddedItems.Count > 0
                                                                                              then
                                                                                                  let skill =
                                                                                                      args.AddedItems.[0]
                                                                                                      :?> Skill

                                                                                                  selectedSkillIndexState
                                                                                                      .Set(
                                                                                                          skillsState
                                                                                                              .Current
                                                                                                              .IndexOf(
                                                                                                                  skill
                                                                                                              )
                                                                                                      )

                                                                                                  newSkillNameState
                                                                                                      .Set(skill.Name)

                                                                                                  newSkillKeywordsState
                                                                                                      .Set(
                                                                                                          String.Join(
                                                                                                              ", ",
                                                                                                              skill.Keywords
                                                                                                          )
                                                                                                      )
                                                                                              else
                                                                                                  selectedSkillIndexState
                                                                                                      .Set(-1)

                                                                                                  newSkillNameState
                                                                                                      .Set("")

                                                                                                  newSkillKeywordsState
                                                                                                      .Set(""))
                                                                                      ListBox.itemTemplate (
                                                                                          DataTemplateView<_>.create
                                                                                              (fun (skill: Skill) ->
                                                                                                  TextBlock.create
                                                                                                      [ TextBlock.text
                                                                                                            skill.Name ])
                                                                                      ) ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "Skill name"
                                                                                      TextBox.text
                                                                                          newSkillNameState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          newSkillNameState.Set t) ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark
                                                                                          "Keywords (comma separated)"
                                                                                      TextBox.text
                                                                                          newSkillKeywordsState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          newSkillKeywordsState.Set t) ]
                                                                                StackPanel.create
                                                                                    [ StackPanel.orientation
                                                                                          Orientation.Horizontal
                                                                                      StackPanel.horizontalAlignment
                                                                                          HorizontalAlignment.Right
                                                                                      StackPanel.spacing 4.0
                                                                                      StackPanel.children
                                                                                          [ Button.create
                                                                                                [ Button.content "Add"
                                                                                                  Button.onClick
                                                                                                      (fun _ ->
                                                                                                          if
                                                                                                              not (
                                                                                                                  String.IsNullOrWhiteSpace
                                                                                                                      newSkillNameState.Current
                                                                                                              )
                                                                                                          then
                                                                                                              let keywords =
                                                                                                                  if
                                                                                                                      String.IsNullOrWhiteSpace
                                                                                                                          newSkillKeywordsState.Current
                                                                                                                  then
                                                                                                                      []
                                                                                                                  else
                                                                                                                      newSkillKeywordsState
                                                                                                                          .Current
                                                                                                                          .Split(
                                                                                                                              [| ','
                                                                                                                                 ';' |],
                                                                                                                              StringSplitOptions.RemoveEmptyEntries
                                                                                                                          )
                                                                                                                      |> Array.toList

                                                                                                              let skill =
                                                                                                                  Skill(
                                                                                                                      newSkillNameState.Current,
                                                                                                                      keywords
                                                                                                                  )

                                                                                                              skillsState.Current.Add
                                                                                                                  skill

                                                                                                              newSkillNameState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newSkillKeywordsState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )) ]
                                                                                            Button.create
                                                                                                [ Button.content
                                                                                                      "Update"
                                                                                                  Button.isEnabled (
                                                                                                      selectedSkillIndexState.Current
                                                                                                      >= 0
                                                                                                  )
                                                                                                  Button.onClick
                                                                                                      (fun _ ->
                                                                                                          if
                                                                                                              selectedSkillIndexState.Current
                                                                                                              >= 0
                                                                                                          then
                                                                                                              let skill =
                                                                                                                  skillsState.Current.[selectedSkillIndexState.Current]

                                                                                                              skill.Name <-
                                                                                                                  newSkillNameState.Current

                                                                                                              skill.Keywords <-
                                                                                                                  if
                                                                                                                      String.IsNullOrWhiteSpace
                                                                                                                          newSkillKeywordsState.Current
                                                                                                                  then
                                                                                                                      []
                                                                                                                  else
                                                                                                                      newSkillKeywordsState
                                                                                                                          .Current
                                                                                                                          .Split(
                                                                                                                              [| ','
                                                                                                                                 ';' |],
                                                                                                                              StringSplitOptions.RemoveEmptyEntries
                                                                                                                          )
                                                                                                                      |> Array.toList

                                                                                                              skillsState.Current.[selectedSkillIndexState.Current] <-
                                                                                                                  skill

                                                                                                              skillsState.Current
                                                                                                              |> ignore) ]
                                                                                            Button.create
                                                                                                [ Button.content
                                                                                                      "Delete"
                                                                                                  Button.isEnabled (
                                                                                                      selectedSkillIndexState.Current
                                                                                                      >= 0
                                                                                                  )
                                                                                                  Button.onClick
                                                                                                      (fun _ ->
                                                                                                          if
                                                                                                              selectedSkillIndexState.Current
                                                                                                              >= 0
                                                                                                          then
                                                                                                              skillsState.Current.RemoveAt
                                                                                                                  selectedSkillIndexState.Current

                                                                                                              selectedSkillIndexState
                                                                                                                  .Set(
                                                                                                                      -1
                                                                                                                  )

                                                                                                              newSkillNameState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newSkillKeywordsState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )) ] ] ] ] ]
                                                                ) ]

                                                          // Certifications Expander
                                                          Expander.create
                                                              [ Expander.header "Certifications"
                                                                Expander.horizontalAlignment
                                                                    HorizontalAlignment.Stretch
                                                                Expander.isExpanded false
                                                                Expander.content (
                                                                    StackPanel.create
                                                                        [ StackPanel.orientation Orientation.Vertical
                                                                          StackPanel.spacing 2.0
                                                                          StackPanel.children
                                                                              [ ListBox.create
                                                                                    [ ListBox.dataItems
                                                                                          certificationsState.Current
                                                                                      ListBox.selectionMode
                                                                                          SelectionMode.Single
                                                                                      ListBox.onSelectionChanged
                                                                                          (fun args ->
                                                                                              if
                                                                                                  args.AddedItems.Count > 0
                                                                                              then
                                                                                                  let cert =
                                                                                                      args.AddedItems.[0]
                                                                                                      :?> Certification

                                                                                                  selectedCertIndexState
                                                                                                      .Set(
                                                                                                          certificationsState
                                                                                                              .Current
                                                                                                              .IndexOf(
                                                                                                                  cert
                                                                                                              )
                                                                                                      )

                                                                                                  newCertTitleState
                                                                                                      .Set(cert.Title)

                                                                                                  newCertIssuerState
                                                                                                      .Set(
                                                                                                          cert.Issuer
                                                                                                      )

                                                                                                  newCertDateState.Set(
                                                                                                      cert.Date
                                                                                                  )

                                                                                                  newCertLabelState
                                                                                                      .Set(cert.Label)

                                                                                                  newCertWebsiteState
                                                                                                      .Set(
                                                                                                          if
                                                                                                              cert.Website
                                                                                                              <> null
                                                                                                          then
                                                                                                              cert.Website.OriginalString
                                                                                                          else
                                                                                                              ""
                                                                                                      )
                                                                                              else
                                                                                                  selectedCertIndexState
                                                                                                      .Set(-1)

                                                                                                  newCertTitleState
                                                                                                      .Set("")

                                                                                                  newCertIssuerState
                                                                                                      .Set("")

                                                                                                  newCertDateState.Set(
                                                                                                      ""
                                                                                                  )

                                                                                                  newCertLabelState
                                                                                                      .Set("")

                                                                                                  newCertWebsiteState
                                                                                                      .Set(""))
                                                                                      ListBox.itemTemplate (
                                                                                          DataTemplateView<_>.create
                                                                                              (fun
                                                                                                  (cert: Certification) ->
                                                                                                  TextBlock.create
                                                                                                      [ TextBlock.text (
                                                                                                            sprintf
                                                                                                                "%s - %s"
                                                                                                                cert.Title
                                                                                                                cert.Issuer
                                                                                                        ) ])
                                                                                      ) ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "Title"
                                                                                      TextBox.text
                                                                                          newCertTitleState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          newCertTitleState.Set t) ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "Issuer"
                                                                                      TextBox.text
                                                                                          newCertIssuerState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          newCertIssuerState.Set t) ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "Date"
                                                                                      TextBox.text
                                                                                          newCertDateState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          newCertDateState.Set t) ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "Label"
                                                                                      TextBox.text
                                                                                          newCertLabelState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          newCertLabelState.Set t) ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "Website"
                                                                                      TextBox.text
                                                                                          newCertWebsiteState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          newCertWebsiteState.Set t)
                                                                                      if
                                                                                          newCertWebsiteState.Current
                                                                                          <> ""
                                                                                          && not (
                                                                                              isValidUrl
                                                                                                  newCertWebsiteState.Current
                                                                                          )
                                                                                      then
                                                                                          TextBox.classes [ "invalid" ] ]
                                                                                StackPanel.create
                                                                                    [ StackPanel.orientation
                                                                                          Orientation.Horizontal
                                                                                      StackPanel.horizontalAlignment
                                                                                          HorizontalAlignment.Right
                                                                                      StackPanel.spacing 4.0
                                                                                      StackPanel.children
                                                                                          [ Button.create
                                                                                                [ Button.content "Add"
                                                                                                  Button.onClick
                                                                                                      (fun _ ->
                                                                                                          if
                                                                                                              not (
                                                                                                                  String.IsNullOrWhiteSpace
                                                                                                                      newCertTitleState.Current
                                                                                                              )
                                                                                                          then
                                                                                                              let website =
                                                                                                                  if
                                                                                                                      String.IsNullOrWhiteSpace
                                                                                                                          newCertWebsiteState.Current
                                                                                                                  then
                                                                                                                      null
                                                                                                                  else
                                                                                                                      Uri(
                                                                                                                          newCertWebsiteState.Current
                                                                                                                      )

                                                                                                              let cert =
                                                                                                                  Certification(
                                                                                                                      newCertTitleState.Current,
                                                                                                                      newCertIssuerState.Current,
                                                                                                                      newCertDateState.Current,
                                                                                                                      newCertLabelState.Current,
                                                                                                                      website
                                                                                                                  )

                                                                                                              certificationsState
                                                                                                                  .Current
                                                                                                                  .Add(
                                                                                                                      cert
                                                                                                                  )

                                                                                                              newCertTitleState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newCertIssuerState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newCertDateState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newCertLabelState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newCertWebsiteState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )) ]
                                                                                            Button.create
                                                                                                [ Button.content
                                                                                                      "Update"
                                                                                                  Button.isEnabled (
                                                                                                      selectedCertIndexState.Current
                                                                                                      >= 0
                                                                                                  )
                                                                                                  Button.onClick
                                                                                                      (fun _ ->
                                                                                                          if
                                                                                                              selectedCertIndexState.Current
                                                                                                              >= 0
                                                                                                          then
                                                                                                              let cert =
                                                                                                                  certificationsState.Current.[selectedCertIndexState.Current]

                                                                                                              cert.Title <-
                                                                                                                  newCertTitleState.Current

                                                                                                              cert.Issuer <-
                                                                                                                  newCertIssuerState.Current

                                                                                                              cert.Date <-
                                                                                                                  newCertDateState.Current

                                                                                                              cert.Label <-
                                                                                                                  newCertLabelState.Current

                                                                                                              cert.Website <-
                                                                                                                  if
                                                                                                                      String.IsNullOrWhiteSpace
                                                                                                                          newCertWebsiteState.Current
                                                                                                                  then
                                                                                                                      null
                                                                                                                  else
                                                                                                                      Uri(
                                                                                                                          newCertWebsiteState.Current
                                                                                                                      )

                                                                                                              certificationsState.Current.[selectedCertIndexState.Current] <-
                                                                                                                  cert

                                                                                                              certificationsState.Current
                                                                                                              |> ignore) ]
                                                                                            Button.create
                                                                                                [ Button.content
                                                                                                      "Delete"
                                                                                                  Button.isEnabled (
                                                                                                      selectedCertIndexState.Current
                                                                                                      >= 0
                                                                                                  )
                                                                                                  Button.onClick
                                                                                                      (fun _ ->
                                                                                                          if
                                                                                                              selectedCertIndexState.Current
                                                                                                              >= 0
                                                                                                          then
                                                                                                              certificationsState
                                                                                                                  .Current
                                                                                                                  .RemoveAt(
                                                                                                                      selectedCertIndexState.Current
                                                                                                                  )

                                                                                                              selectedCertIndexState
                                                                                                                  .Set(
                                                                                                                      -1
                                                                                                                  )

                                                                                                              newCertTitleState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newCertIssuerState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newCertDateState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newCertLabelState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newCertWebsiteState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )) ] ] ] ] ]
                                                                ) ]

                                                          // Education Expander
                                                          Expander.create
                                                              [ Expander.header "Education"
                                                                Expander.horizontalAlignment
                                                                    HorizontalAlignment.Stretch
                                                                Expander.isExpanded false
                                                                Expander.content (
                                                                    StackPanel.create
                                                                        [ StackPanel.orientation Orientation.Vertical
                                                                          StackPanel.spacing 2.0
                                                                          StackPanel.children
                                                                              [ ListBox.create
                                                                                    [ ListBox.dataItems
                                                                                          educationsState.Current
                                                                                      ListBox.selectionMode
                                                                                          SelectionMode.Single
                                                                                      ListBox.onSelectionChanged
                                                                                          (fun args ->
                                                                                              if
                                                                                                  args.AddedItems.Count > 0
                                                                                              then
                                                                                                  let edu =
                                                                                                      args.AddedItems.[0]
                                                                                                      :?> Education

                                                                                                  selectedEduIndexState
                                                                                                      .Set(
                                                                                                          educationsState
                                                                                                              .Current
                                                                                                              .IndexOf(
                                                                                                                  edu
                                                                                                              )
                                                                                                      )

                                                                                                  newEduSchoolState
                                                                                                      .Set(edu.School)

                                                                                                  newEduDegreeState
                                                                                                      .Set(edu.Degree)

                                                                                                  newEduAreaState.Set(
                                                                                                      edu.Area
                                                                                                  )

                                                                                                  newEduGradeState.Set(
                                                                                                      edu.Grade
                                                                                                  )

                                                                                                  newEduLocationState
                                                                                                      .Set(
                                                                                                          edu.Location
                                                                                                      )

                                                                                                  newEduPeriodState
                                                                                                      .Set(edu.Period)

                                                                                                  newEduWebsiteState
                                                                                                      .Set(
                                                                                                          edu.Website
                                                                                                      )
                                                                                              else
                                                                                                  selectedEduIndexState
                                                                                                      .Set(-1)

                                                                                                  newEduSchoolState
                                                                                                      .Set("")

                                                                                                  newEduDegreeState
                                                                                                      .Set("")

                                                                                                  newEduAreaState.Set(
                                                                                                      ""
                                                                                                  )

                                                                                                  newEduGradeState.Set(
                                                                                                      ""
                                                                                                  )

                                                                                                  newEduLocationState
                                                                                                      .Set("")

                                                                                                  newEduPeriodState
                                                                                                      .Set("")

                                                                                                  newEduWebsiteState
                                                                                                      .Set(""))
                                                                                      ListBox.itemTemplate (
                                                                                          DataTemplateView<_>.create
                                                                                              (fun (edu: Education) ->
                                                                                                  TextBlock.create
                                                                                                      [ TextBlock.text (
                                                                                                            sprintf
                                                                                                                "%s - %s"
                                                                                                                edu.School
                                                                                                                edu.Degree
                                                                                                        ) ])
                                                                                      ) ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "School"
                                                                                      TextBox.text
                                                                                          newEduSchoolState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          newEduSchoolState.Set t) ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "Degree"
                                                                                      TextBox.text
                                                                                          newEduDegreeState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          newEduDegreeState.Set t) ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "Area"
                                                                                      TextBox.text
                                                                                          newEduAreaState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          newEduAreaState.Set t) ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "Grade"
                                                                                      TextBox.text
                                                                                          newEduGradeState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          newEduGradeState.Set t) ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "Location"
                                                                                      TextBox.text
                                                                                          newEduLocationState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          newEduLocationState.Set t) ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "Period"
                                                                                      TextBox.text
                                                                                          newEduPeriodState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          newEduPeriodState.Set t) ]
                                                                                TextBox.create
                                                                                    [ TextBox.watermark "Website"
                                                                                      TextBox.text
                                                                                          newEduWebsiteState.Current
                                                                                      TextBox.onTextChanged (fun t ->
                                                                                          newEduWebsiteState.Set t) ]
                                                                                StackPanel.create
                                                                                    [ StackPanel.orientation
                                                                                          Orientation.Horizontal
                                                                                      StackPanel.horizontalAlignment
                                                                                          HorizontalAlignment.Right
                                                                                      StackPanel.spacing 4.0
                                                                                      StackPanel.children
                                                                                          [ Button.create
                                                                                                [ Button.content "Add"
                                                                                                  Button.onClick
                                                                                                      (fun _ ->
                                                                                                          if
                                                                                                              not (
                                                                                                                  String.IsNullOrWhiteSpace
                                                                                                                      newEduSchoolState.Current
                                                                                                              )
                                                                                                          then
                                                                                                              let edu =
                                                                                                                  Education(
                                                                                                                      newEduSchoolState.Current,
                                                                                                                      newEduDegreeState.Current,
                                                                                                                      newEduAreaState.Current,
                                                                                                                      newEduGradeState.Current,
                                                                                                                      newEduLocationState.Current,
                                                                                                                      newEduPeriodState.Current,
                                                                                                                      newEduWebsiteState.Current
                                                                                                                  )

                                                                                                              educationsState
                                                                                                                  .Current
                                                                                                                  .Add(
                                                                                                                      edu
                                                                                                                  )

                                                                                                              newEduSchoolState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newEduDegreeState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newEduAreaState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newEduGradeState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newEduLocationState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newEduPeriodState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newEduWebsiteState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )) ]
                                                                                            Button.create
                                                                                                [ Button.content
                                                                                                      "Update"
                                                                                                  Button.isEnabled (
                                                                                                      selectedEduIndexState.Current
                                                                                                      >= 0
                                                                                                  )
                                                                                                  Button.onClick
                                                                                                      (fun _ ->
                                                                                                          if
                                                                                                              selectedEduIndexState.Current
                                                                                                              >= 0
                                                                                                          then
                                                                                                              let edu =
                                                                                                                  educationsState.Current.[selectedEduIndexState.Current]

                                                                                                              edu.School <-
                                                                                                                  newEduSchoolState.Current

                                                                                                              edu.Degree <-
                                                                                                                  newEduDegreeState.Current

                                                                                                              edu.Area <-
                                                                                                                  newEduAreaState.Current

                                                                                                              edu.Grade <-
                                                                                                                  newEduGradeState.Current

                                                                                                              edu.Location <-
                                                                                                                  newEduLocationState.Current

                                                                                                              edu.Period <-
                                                                                                                  newEduPeriodState.Current

                                                                                                              edu.Website <-
                                                                                                                  newEduWebsiteState.Current

                                                                                                              educationsState.Current.[selectedEduIndexState.Current] <-
                                                                                                                  edu

                                                                                                              educationsState.Current
                                                                                                              |> ignore) ]
                                                                                            Button.create
                                                                                                [ Button.content
                                                                                                      "Delete"
                                                                                                  Button.isEnabled (
                                                                                                      selectedEduIndexState.Current
                                                                                                      >= 0
                                                                                                  )
                                                                                                  Button.onClick
                                                                                                      (fun _ ->
                                                                                                          if
                                                                                                              selectedEduIndexState.Current
                                                                                                              >= 0
                                                                                                          then
                                                                                                              educationsState
                                                                                                                  .Current
                                                                                                                  .RemoveAt(
                                                                                                                      selectedEduIndexState.Current
                                                                                                                  )

                                                                                                              selectedEduIndexState
                                                                                                                  .Set(
                                                                                                                      -1
                                                                                                                  )

                                                                                                              newEduSchoolState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newEduDegreeState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newEduAreaState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newEduGradeState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newEduLocationState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newEduPeriodState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )

                                                                                                              newEduWebsiteState
                                                                                                                  .Set(
                                                                                                                      ""
                                                                                                                  )) ] ] ] ] ]
                                                                ) ] ] ]
                                          ) ]

                                    // bottom controls
                                    StackPanel.create
                                        [ Grid.row 1
                                          StackPanel.orientation Orientation.Horizontal
                                          StackPanel.horizontalAlignment HorizontalAlignment.Right
                                          StackPanel.verticalAlignment VerticalAlignment.Bottom
                                          StackPanel.children
                                              [ ComboBox.create [ 
                                                ComboBox.dataItems xsltFilesState.Current;
                                                ComboBox.selectedIndex selectedXsltIndexState.Current
                                                ComboBox.onSelectedIndexChanged (fun index ->
                                                    selectedXsltIndexState.Set index
                                                    index |> xsltFilesState.Current.GetValue :?> XsltFile |> Some |> selectedXsltState.Set
                                                )
                                                ComboBox.itemTemplate (
                                                    DataTemplateView<_>.create(fun (f:XsltFile)->
                                                        TextBlock.create [TextBlock.text f.Name]
                                                    )
                                                )
                                                ]
                                                Button.create
                                                    [ Button.content "Load from XML"
                                                      Button.onClick (fun _ -> loadFromXml states) ]
                                                DropDownButton.create
                                                    [ DropDownButton.content "Save As"
                                                      DropDownButton.flyout (
                                                          MenuFlyout.create
                                                              [ MenuFlyout.placement
                                                                    PlacementMode.BottomEdgeAlignedRight
                                                                MenuFlyout.viewItems
                                                                    [ MenuItem.create
                                                                          [ MenuItem.header "XML"
                                                                            MenuItem.onClick (fun _ ->
                                                                                startGenerateResume Xml) ]
                                                                      MenuItem.create
                                                                          [ MenuItem.header "HTML"
                                                                            MenuItem.isEnabled selectedXsltState.Current.IsSome
                                                                            MenuItem.onClick (fun _ ->
                                                                                startGenerateResume Html) ]
                                                                      MenuItem.create
                                                                          [ MenuItem.header "PDF"
                                                                            MenuItem.isEnabled selectedXsltState.Current.IsSome
                                                                            MenuItem.onClick (fun _ ->
                                                                                startGenerateResume Pdf) ] ] ]
                                                      ) ] ] ] ] ] ] ])


type MainWindow() as this =
    inherit HostWindow()

    do
        let invalidTextBoxStyle =
            let style = Style(fun x -> x.OfType<TextBox>().Class "invalid")
            style.Setters.Add(Setter(TextBox.BorderBrushProperty, Brushes.Red))
            style :> IStyle

        base.Title <- "Resume Generator"
        base.Width <- 800.0
        base.Height <- 600.0
        base.Icon <- new WindowIcon(new Bitmap(Path.Combine(__SOURCE_DIRECTORY__, "..", "img", "Fsharp_logo.png")))
        base.Styles.Add invalidTextBoxStyle
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
            printfn "App running..."
        | _ -> ()

let app =
    AppBuilder.Configure<App>().UsePlatformDetect().StartWithClassicDesktopLifetime([||])
