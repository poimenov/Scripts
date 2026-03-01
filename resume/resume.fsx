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
open Avalonia.FuncUI.Types
open Avalonia.FuncUI.Hosts
open Avalonia.Layout
open Avalonia.Media.Imaging
open Avalonia.Platform.Storage
open PuppeteerSharp
open PuppeteerSharp.Media
open Avalonia.Controls.Primitives

type GenerateToFormat =
    | Pdf
    | Xml
    | Html

let emailRegex = Regex("^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)
let phoneRegex = Regex("^\+?[0-9\s\-()]+$", RegexOptions.Compiled)

let getMainWindow() =
    match Application.Current.ApplicationLifetime with
    | :? ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime as desktop -> desktop.MainWindow
    | _ -> null

let choosePicturePath () =
    async {
        let win = getMainWindow()
        if isNull win then return None
        else
            let options = FilePickerOpenOptions()
            options.AllowMultiple <- false
            options.Title <- "Choose a picture"
            options.FileTypeFilter <- ["*.jpg"; "*.jpeg"; "*.png"; "*.bmp"; "*.gif"] |> List.map (fun pattern -> FilePickerFileType pattern)
            let! res = win.StorageProvider.OpenFilePickerAsync options |> Async.AwaitTask
            if Seq.isEmpty res then return None
            else return Some (Seq.head res).Path
    }

let chooseFolder () =
    async {
        let win = getMainWindow()
        if isNull win then return None
        else
            let options = FolderPickerOpenOptions()
            let! res = win.StorageProvider.OpenFolderPickerAsync options |> Async.AwaitTask
            if Seq.isEmpty res then return None
            else return Some (Seq.head res).Path
    }

// similar helper to select an XML file for importing
let chooseXmlPath () =
    async {
        let win = getMainWindow()
        if isNull win then return None
        else
            let options = FilePickerOpenOptions()
            options.AllowMultiple <- false
            options.Title <- "Choose resume XML"
            options.FileTypeFilter <- ["*.xml"] |> List.map (fun pattern -> FilePickerFileType pattern)
            let! res = win.StorageProvider.OpenFilePickerAsync options |> Async.AwaitTask
            if Seq.isEmpty res then return None
            else return Some (Seq.head res).Path
    }

// helper that tries to create a Uri from an arbitrary string
let tryMakeUri (s:string) : Uri option =
    if String.IsNullOrWhiteSpace s then None
    else
        try
            let u = Uri(s, UriKind.RelativeOrAbsolute)
            if not u.IsAbsoluteUri && File.Exists(s) then
                Some(Uri(Path.GetFullPath(s)))
            else
                Some u
        with _ ->
            if File.Exists(s) then Some(Uri(Path.GetFullPath(s)))
            else None

let getXmlDoc (picturePath:string,
                  name:string,
                  headline:string,
                  email:string,
                  phone:string,
                  location:string,
                  links:ObservableCollection<string>,
                  summary:string) =
    let embedPicture (path:string) : string =
        if String.IsNullOrWhiteSpace path then ""
        else
            try
                // try to interpret the string as a URI first
                let uri = Uri path
                let filePath = if uri.IsFile then uri.LocalPath else path
                if File.Exists filePath then
                    let bytes = File.ReadAllBytes filePath
                    let mime =
                        match Path.GetExtension(filePath).ToLowerInvariant() with
                        | ".jpg" | ".jpeg" -> "image/jpeg"
                        | ".png" -> "image/png"
                        | ".gif" -> "image/gif"
                        | ".bmp" -> "image/bmp"
                        | _ -> "application/octet-stream"
                    let base64 = Convert.ToBase64String(bytes)
                    sprintf "data:%s;base64,%s" mime base64
                else
                    // if file doesn't exist, just return original string
                    path
            with
            | _ -> path                  
    let pictureValue = embedPicture picturePath
    let doc =
        XDocument(
            XElement("resume",
                XElement("picturePath", pictureValue),
                XElement("name", name),
                XElement("headline", headline),
                XElement("email", email),
                XElement("phone", phone),
                XElement("location", location),
                XElement("links",
                    links |> Seq.map (fun l -> XElement("link", l)) |> Seq.toArray
                ),
                XElement("summary", summary)
            )
        )
    doc

let transformXmlToHtml (doc:XDocument) =
    let xsltPath = Path.Combine(__SOURCE_DIRECTORY__, "xslt", "resume-to-html.xslt")
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
        let pdfOptions = PdfOptions(Format = PaperFormat.A4, 
            DisplayHeaderFooter = false, 
            MarginOptions = new MarginOptions(Top = "5mm", Bottom = "5mm", Left = "5mm", Right = "5mm"))
        do! page.PdfAsync(outputPath, pdfOptions)
    }    

[<AbstractClass; Sealed>]
type Views =
    static member main() =
        Component(fun ctx ->
            // states
            let mkExpander (attrs:IAttr<Expander> list) : IView<Expander> =
                ViewBuilder.Create<Expander> attrs
            let pictureUriState : IWritable<Uri option> = ctx.useState None
            let nameState = ctx.useState ""
            let headlineState = ctx.useState ""
            let emailState = ctx.useState ""
            let phoneState = ctx.useState ""
            let locationState = ctx.useState ""
            let linksState = ctx.useState (ObservableCollection<string>())
            let summaryState = ctx.useState ""
            let outputFolderState = ctx.useState __SOURCE_DIRECTORY__
            let newLinkState = ctx.useState ""

            let imgSource =
                match pictureUriState.Current with
                | None -> null
                | Some uri ->
                    let s = uri.OriginalString
                    if s.StartsWith("data:") then
                        // data URI: decode base64 and create bitmap from stream
                        try
                            let comma = s.IndexOf(',')
                            if comma >= 0 then
                                let base64 = s.Substring(comma + 1)
                                let bytes = Convert.FromBase64String(base64)
                                use ms = new MemoryStream(bytes)
                                new Bitmap(ms)
                            else null
                        with _ -> null
                    else
                        let path = HttpUtility.UrlDecode uri.AbsolutePath
                        if not (String.IsNullOrWhiteSpace path) && File.Exists path then
                            new Bitmap(path)
                        else
                            null

            let generateResume (format:GenerateToFormat) =
                async {
                    if String.IsNullOrWhiteSpace outputFolderState.Current then
                        let! opt = chooseFolder()
                        opt |> Option.iter (fun f -> outputFolderState.Set f.AbsolutePath)
                    if not (String.IsNullOrWhiteSpace outputFolderState.Current) then
                        let xml = getXmlDoc (
                                        pictureUriState.Current |> Option.map (fun u -> u.OriginalString) |> Option.defaultValue "",
                                        nameState.Current,
                                        headlineState.Current,
                                        emailState.Current,
                                        phoneState.Current,
                                        locationState.Current,
                                        linksState.Current,
                                        summaryState.Current)
                        match format with
                        | GenerateToFormat.Xml -> 
                            let xmlPath = Path.Combine(outputFolderState.Current, "resume.xml")
                            xml.Save xmlPath
                        | GenerateToFormat.Html ->
                            let html = transformXmlToHtml xml
                            let htmlPath = Path.Combine(outputFolderState.Current, "resume.html")
                            File.WriteAllText(htmlPath, html)
                        | GenerateToFormat.Pdf ->
                             let html = transformXmlToHtml xml
                             let pdfPath = Path.Combine(outputFolderState.Current, "resume.pdf")
                             do! generatePdfFromHtml(html, pdfPath) |> Async.AwaitTask
                } |> Async.StartImmediate

            // load data from an existing resume XML file and populate UI states
            let loadFromXml () =
                async {
                    let! opt = chooseXmlPath()
                    match opt with
                    | Some uri ->
                        // chooseXmlPath returns a Uri, so convert to filesystem path
                        let path =
                            if uri.IsFile then uri.LocalPath
                            else uri.OriginalString
                        try
                            let doc = XDocument.Load(path)
                            let get name =
                                let el = doc.Root.Element(XName.Get name)
                                if isNull el then "" else el.Value
                            // picture may be embedded data URI or file path
                            let pic = get "picturePath"
                            if not (String.IsNullOrWhiteSpace pic) then
                                // set state directly with the option result
                                pictureUriState.Set(tryMakeUri pic)
                            else
                                pictureUriState.Set(None)
                            nameState.Set(get "name")
                            headlineState.Set(get "headline")
                            emailState.Set(get "email")
                            phoneState.Set(get "phone")
                            locationState.Set(get "location")
                            // clear and repopulate links
                            linksState.Current.Clear()
                            let linksEl = doc.Root.Element(XName.Get "links")
                            if not (isNull linksEl) then
                                for linkNode in linksEl.Elements(XName.Get "link") do
                                    linksState.Current.Add(linkNode.Value)
                            summaryState.Set(get "summary")
                        with ex ->
                            // just log; UI doesn't have a logger
                            printfn "Failed to load XML '%s': %s" path ex.Message
                    | None -> ()
                } |> Async.StartImmediate

            DockPanel.create [
                DockPanel.children [
                    Grid.create [
                        Grid.dock Dock.Top
                        Grid.margin 4
                        Grid.columnDefinitions "*"
                        Grid.rowDefinitions "*, Auto"
                        Grid.children [
                            ScrollViewer.create [
                                Grid.row 0
                                ScrollViewer.content (
                                    // compute each expander view ahead of time
                                    let pictureExpander =
                                        mkExpander [
                                            Expander.header "Picture"
                                            Expander.horizontalAlignment HorizontalAlignment.Stretch
                                            Expander.content (
                                                StackPanel.create [
                                                    StackPanel.orientation Orientation.Horizontal
                                                    StackPanel.verticalAlignment VerticalAlignment.Center
                                                    StackPanel.spacing 2.0
                                                    StackPanel.children [
                                                        Image.create [
                                                            Image.source imgSource
                                                            Image.maxHeight 90.0
                                                            Image.maxWidth 90.0
                                                        ]                                                                
                                                        TextBox.create [
                                                            TextBox.watermark "Image path"
                                                            TextBox.width 300.0
                                                            TextBox.height 30.0
                                                            TextBox.text (
                                                                pictureUriState.Current
                                                                |> Option.map (fun u -> u.OriginalString)
                                                                |> Option.defaultValue "")
                                                            TextBox.onTextChanged (fun t ->
                                                                match tryMakeUri t with
                                                                | Some u -> pictureUriState.Set (Some u)
                                                                | None -> pictureUriState.Set None)
                                                        ]
                                                        Button.create [
                                                            Button.content "..."
                                                            Button.tip "Choose picture"
                                                            Button.onClick (fun _ ->
                                                                async {
                                                                    let! opt = choosePicturePath()
                                                                    opt |> Option.iter (fun p -> pictureUriState.Set (Some p))
                                                                } |> Async.StartImmediate)
                                                        ]
                                                    ]
                                                ]
                                            )
                                        ]

                                    let basicInfoExpander =
                                        mkExpander [
                                            Expander.header "Basic Information"
                                            Expander.horizontalAlignment HorizontalAlignment.Stretch
                                            Expander.content (
                                                StackPanel.create [
                                                    StackPanel.orientation Orientation.Vertical
                                                    StackPanel.spacing 2.0
                                                    StackPanel.children [
                                                        TextBox.create [
                                                            TextBox.watermark "Name"
                                                            TextBox.text nameState.Current
                                                            TextBox.onTextChanged (fun t -> nameState.Set t)
                                                        ]
                                                        TextBox.create [
                                                            TextBox.watermark "Headline"
                                                            TextBox.text headlineState.Current
                                                            TextBox.onTextChanged (fun t -> headlineState.Set t)
                                                        ]
                                                        TextBox.create [
                                                            TextBox.watermark "Email"
                                                            TextBox.text emailState.Current
                                                            TextBox.onTextChanged (fun t -> emailState.Set t)
                                                            if emailState.Current <> "" && not (emailRegex.IsMatch emailState.Current) then
                                                                TextBox.classes [ "invalid" ]
                                                        ]
                                                        TextBox.create [
                                                            TextBox.watermark "Phone"
                                                            TextBox.text phoneState.Current
                                                            TextBox.onTextChanged (fun t -> phoneState.Set t)
                                                            if phoneState.Current <> "" && not (phoneRegex.IsMatch phoneState.Current) then
                                                                TextBox.classes [ "invalid" ]
                                                        ]
                                                        TextBox.create [
                                                            TextBox.watermark "Location"
                                                            TextBox.text locationState.Current
                                                            TextBox.onTextChanged (fun t -> locationState.Set t)
                                                        ]
                                                        TextBlock.create [ TextBlock.text "Links" ]
                                                        ListBox.create [ ListBox.dataItems linksState.Current ]
                                                        StackPanel.create [
                                                            StackPanel.orientation Orientation.Horizontal
                                                            StackPanel.spacing 2.0
                                                            StackPanel.children [
                                                                TextBox.create [
                                                                    TextBox.watermark "New link"
                                                                    TextBox.width 300.0
                                                                    TextBox.text newLinkState.Current
                                                                    TextBox.onTextChanged (fun t -> newLinkState.Set t)
                                                                ]
                                                                Button.create [
                                                                    Button.content "Add"
                                                                    Button.onClick (fun _ ->
                                                                        if not (String.IsNullOrWhiteSpace newLinkState.Current) then
                                                                            linksState.Current.Add newLinkState.Current
                                                                            newLinkState.Set ""
                                                                    )
                                                                ]
                                                            ]
                                                        ]
                                                    ]
                                                ]
                                            )
                                        ]

                                    let summaryExpander =
                                        mkExpander [
                                            Expander.header "Summary"
                                            Expander.horizontalAlignment HorizontalAlignment.Stretch
                                            Expander.content (
                                                TextBox.create [
                                                    TextBox.watermark "HTML summary"
                                                    TextBox.text summaryState.Current
                                                    TextBox.onTextChanged (fun t -> summaryState.Set t)
                                                    TextBox.acceptsReturn true
                                                    TextBox.height 150.0
                                                    TextBox.verticalScrollBarVisibility ScrollBarVisibility.Auto
                                                    TextBox.horizontalScrollBarVisibility ScrollBarVisibility.Auto
                                                ]
                                            )
                                        ]

                                    let childrenList : IView list = [
                                        pictureExpander :> IView
                                        basicInfoExpander :> IView
                                        summaryExpander :> IView
                                    ]

                                    StackPanel.create [
                                        StackPanel.orientation Orientation.Vertical
                                        StackPanel.children childrenList
                                    ]
                                    )
                                ]   // end ScrollViewer.create
        
                            // bottom controls
                            StackPanel.create [
                                Grid.row 1
                                StackPanel.orientation Orientation.Horizontal
                                StackPanel.horizontalAlignment HorizontalAlignment.Right
                                StackPanel.verticalAlignment VerticalAlignment.Bottom
                                StackPanel.children [
                                    Button.create [
                                        Button.content "Change Output Folder"
                                        Button.tip (sprintf "Current: %s" outputFolderState.Current)
                                        Button.onClick (fun _ ->
                                            async {
                                                let! opt = chooseFolder()
                                                opt |> Option.iter (fun f -> outputFolderState.Set (HttpUtility.UrlDecode f.AbsolutePath))
                                            } |> Async.StartImmediate)
                                    ]                                    
                                    Button.create [
                                        Button.content "Load from XML"
                                        Button.onClick (fun _ -> loadFromXml())
                                    ]
                                    SplitButton.create [
                                        SplitButton.content "Save As"
                                        SplitButton.flyout (
                                            MenuFlyout.create [
                                                MenuFlyout.placement PlacementMode.BottomEdgeAlignedRight
                                                MenuFlyout.dataItems [ Xml; Html; Pdf ]
                                                MenuFlyout.itemTemplate (
                                                    DataTemplateView<_>.create (fun (format: GenerateToFormat) -> 
                                                        MenuItem.create [
                                                            MenuItem.width 60.0
                                                            MenuItem.height 20.0
                                                            MenuItem.padding (Thickness(2.0))
                                                            MenuItem.margin (Thickness(0.0))
                                                            MenuItem.header (match format with Xml -> "XML" | Html -> "HTML" | Pdf -> "PDF")
                                                            MenuItem.onClick (fun _ -> generateResume format)
                                                        ]
                                                    )
                                                )
                                            ]
                                        )]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        )


type MainWindow() as this =
    inherit HostWindow()

    do
        base.Title <- "Resume Generator"
        base.Width <- 800.0
        base.Height <- 500.0
        base.Icon <- new WindowIcon(new Bitmap(Path.Combine(__SOURCE_DIRECTORY__, "../img/Fsharp_logo.png")))
        this.Content <- Views.main ()


type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Add(Themes.Fluent.FluentTheme())
        this.RequestedThemeVariant <- Styling.ThemeVariant.Dark
        this.Styles.Load "avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml"

    override this.OnFrameworkInitializationCompleted() =

        match this.ApplicationLifetime with
        | :? ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime as desktop ->
            desktop.MainWindow <- MainWindow()
            printfn "App running..."
        | _ -> ()

let app =
    AppBuilder
        .Configure<App>()
        .UsePlatformDetect()
        .StartWithClassicDesktopLifetime([||])