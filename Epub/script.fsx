#r "nuget: FSharp.Data,6.4.0"

open FSharp.Data
open System
open System.IO
open System.IO.Compression
open System.Xml.Linq
open System.Xml.Xsl

let author = "Александр Зубченко"
let baseUrl = "https://versii.com/politics/page"
let folderName = "Zubchenko"
let sourceDirectory = __SOURCE_DIRECTORY__
let (+/) path1 path2 = Path.Combine(path1, path2)
let folderPath = sourceDirectory +/ folderName
let basePath = folderPath +/ "EPUB"
let itemsPath = sourceDirectory +/ "items.xml"
let epubPath = sourceDirectory +/ $"{folderName}.epub"
let ns: XNamespace = XNamespace.Get("http://www.w3.org/1999/xhtml")

type Item =
    struct
        val Id: int
        val Author: string
        val Title: string
        val Url: string
        val DateTime: string
        val FileName: string
        val mutable ImageName: string
        val Year: string
        val Month: string

        new(id, author, title, url, dateTime, fileName, imageName, year, month) =
            { Id = id
              Author = author
              Title = title
              Url = url
              DateTime = dateTime
              FileName = fileName
              ImageName = imageName
              Year = year
              Month = month }
    end

let getAsyncHtmlPage (url: string, i: int) =
    async { return! HtmlDocument.AsyncLoad(url + $"/{i}") }

let rec convertHtmlNodeToXElement (node: HtmlNode) : XElement =
    let element = XElement(ns + node.Name(), node.DirectInnerText())

    for attribute in node.Attributes() do
        element.SetAttributeValue(attribute.Name(), attribute.Value())

    for childNode in node.Elements() |> Seq.filter (fun x -> x.Name() <> "") do
        element.Add(convertHtmlNodeToXElement (childNode))

    element

let saveXhtml (content: HtmlNode, path: string, title: string, dateTime: string, imgPath: string) =
    let xElement = convertHtmlNodeToXElement (content)

    if imgPath <> "" then
        let img = xElement.Descendants(ns + "img") |> Seq.head
        img.SetAttributeValue("src", imgPath)

    let xDoc = XDocument.Load(sourceDirectory +/ "PageTemplate.xhtml")

    let titleNode = xDoc.Descendants(ns + "title") |> Seq.head
    titleNode.SetValue(title)

    let h1Node = new XElement(ns + "h1", title)
    xDoc.Descendants(ns + "body") |> Seq.head |> (fun x -> x.AddFirst h1Node)

    let divs = xDoc.Descendants(ns + "div")

    let dateTimeNode = divs |> Seq.tail |> Seq.head
    let dt = dateTime.Split('T').[0]
    dateTimeNode.SetValue($"Дата публикации: {dt} ")

    let aNode =
        new XElement(ns + "a", new XAttribute("href", "nav.xhtml"), "вернуться к содержанию")

    dateTimeNode.Add(aNode)

    let target = divs |> Seq.head
    target.ReplaceWith(xElement)

    xDoc.Save(path)

let getAsyncItem (id: int, htmlItem: HtmlNode) =
    async {
        let h4 = htmlItem.Descendants [ "h4" ] |> Seq.head
        let header = h4.Descendants [ "a" ] |> Seq.head
        let url = header.AttributeValue("href")
        let time = htmlItem.Descendants [ "time" ] |> Seq.head
        let name = url.Split("/").[4]

        let! doc = HtmlDocument.AsyncLoad(url)

        let content =
            doc.Body().Descendants [ "div" ]
            |> Seq.filter (fun x -> x.AttributeValue("class") = "elementor-shortcode")
            |> Seq.item 1

        let title = doc.Body().Descendants [ "h1" ] |> Seq.head

        let imgs =
            content.Descendants [ "img" ]
            |> Seq.filter (fun x -> x.AttributeValue("class").Contains("wp-post-image"))

        let imgPath =
            if (imgs |> Seq.length) > 0 then
                let src = (imgs |> Seq.head).AttributeValue("src")
                [ src; $"img/{name}.jpg" ]
            else
                [ ""; "" ]

        if (imgPath[0] <> "") then
            let! response = Http.AsyncRequest(imgPath[0])

            match response.Body with
            | Binary bytes -> File.WriteAllBytes(basePath +/ imgPath[1], bytes)
            | Text(_) -> ignore ()

        saveXhtml (content, basePath +/ "xhtml" +/ $"{name}.xhtml", title.InnerText(), time.InnerText(), imgPath[1])

        return
            new Item(
                id + 1,
                author,
                header.InnerText(),
                url,
                time.AttributeValue("datetime"),
                $"{name}.xhtml",
                imgPath[1],
                time.AttributeValue("datetime").Split("-").[0],
                time.AttributeValue("datetime").Split("-").[1]
            )
    }


let serializeItem (item: Item) =
    let id = new XElement("Property", new XAttribute("Name", "Id"), item.Id)
    let author = new XElement("Property", new XAttribute("Name", "Author"), item.Author)
    let title = new XElement("Property", new XAttribute("Name", "Title"), item.Title)
    let url = new XElement("Property", new XAttribute("Name", "Url"), item.Url)

    let dateTime =
        new XElement("Property", new XAttribute("Name", "DateTime"), item.DateTime)

    let fileName =
        new XElement("Property", new XAttribute("Name", "FileName"), item.FileName)

    let imageName =
        new XElement("Property", new XAttribute("Name", "ImageName"), item.ImageName)

    let year = new XElement("Property", new XAttribute("Name", "Year"), item.Year)
    let month = new XElement("Property", new XAttribute("Name", "Month"), item.Month)

    let xElements =
        seq {
            id
            author
            title
            url
            dateTime
            fileName
            imageName
            year
            month
        }

    let element = new XElement("Property", xElements)
    element

let serializeItems (items: seq<Item>) =
    let xDoc =
        new XDocument(new XElement("Objects", new XElement("Object", items |> Seq.map serializeItem |> Seq.toArray)))

    xDoc.Save(itemsPath)


let getHtmlItems (page: HtmlDocument) =
    page.Descendants [ "div" ]
    |> Seq.filter (fun x ->
        x.AttributeValue("class") = "jet-posts__inner-content"
        && x.Descendants [ "div" ]
           |> Seq.exists (fun x ->
               x.AttributeValue("class") = "jet-title-fields__item-value"
               && x.InnerText().Contains(author)))

//Start
printfn "Start at %s" (DateTime.Now.ToString())
//Clear the folder and files
if Directory.Exists(folderPath) then
    Directory.Delete(folderPath, true)

if File.Exists(itemsPath) then
    File.Delete(itemsPath)

if File.Exists(epubPath) then
    File.Delete(epubPath)

//Create the folder structure
Directory.CreateDirectory(folderPath) |> ignore
Directory.CreateDirectory(folderPath +/ "META-INF") |> ignore
Directory.CreateDirectory(basePath) |> ignore
Directory.CreateDirectory(basePath +/ "xhtml") |> ignore
Directory.CreateDirectory(basePath +/ "img") |> ignore
Directory.CreateDirectory(basePath +/ "css") |> ignore
//and copy fiiles
File.WriteAllLines(folderPath +/ "mimetype", [| "application/epub+zip" |])
File.Copy(sourceDirectory +/ "container.xml", folderPath +/ "META-INF" +/ "container.xml")
File.Copy(sourceDirectory +/ "style.css", basePath +/ "css" +/ "style.css")
File.Copy(sourceDirectory +/ "titlePage.xhtml", basePath +/ "xhtml" +/ "titlePage.xhtml")

//create files for epub
[ 2..135 ]
|> Seq.map (fun x -> getAsyncHtmlPage (baseUrl, x))
|> fun getPages -> Async.Parallel(getPages, 5)
|> Async.RunSynchronously
|> Seq.collect getHtmlItems
|> Seq.mapi (fun i el -> getAsyncItem (i, el))
|> fun getItems -> Async.Parallel(getItems, 5)
|> Async.RunSynchronously
|> serializeItems

//transform xslt
let xslt = new XslCompiledTransform()
xslt.Load(sourceDirectory +/ "nav.xslt")
xslt.Transform(itemsPath, basePath +/ "xhtml" +/ "nav.xhtml")

xslt.Load(sourceDirectory +/ "package.xslt")
xslt.Transform(itemsPath, basePath +/ "package.opf")

//create epub
ZipFile.CreateFromDirectory(folderPath, epubPath)

//Clear the folder and files
if Directory.Exists(folderPath) then
    Directory.Delete(folderPath, true)

if File.Exists(itemsPath) then
    File.Delete(itemsPath)

//Finish
printfn "Finish at %s" (DateTime.Now.ToString())
printfn $"Done! {epubPath}"
