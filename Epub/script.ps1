# Install the module on demand
If (-not (Get-Module -ErrorAction Ignore -ListAvailable PSParseHTML)) {
    Write-Verbose "Installing PSParseHTML module for the current user..."
    Install-Module -Scope CurrentUser PSParseHTML -ErrorAction Stop
}

function Clear-Mnemonics {
    param (
        [parameter(Mandatory = $true)] [string] $Content  
    )

    return $Content.Replace('&#8230;', '...').Replace('&hellip;', '...').Replace('&ndash;', '-').Replace('&mdash;', '-').Replace('&nbsp;', ' ').Replace('&laquo;', '«').Replace('&raquo;', '»').Replace('&ndash;', '–').Replace('&mdash;', '—').Replace('&bull;', '•').Replace('&copy;', '©').Replace('&reg;', '®').Replace('&trade;', '™').Replace('&quot;', '"').Replace('&apos;', "'")    
}

function  Update-File {
    param (
        [parameter(Mandatory = $true)] [string] $Path,
        [parameter(Mandatory = $true)] [string] $Url
    )

    $Content = Get-Content -Path $Path -Raw
    $Content = $Content.Replace('<span><!--l version="1.0" encoding="UTF-8-->', "<!-- from $Url-->")
    $Content = $Content.Substring(0, $Content.Length - 7)
    Set-Content -Path $Path -Value $Content
}

$baseUrl = 'https://versii.com/politics/page'
$folderName = 'Zubchenko'
$basePath = "$PSScriptRoot/$folderName/EPUB"
$itemsPath = "$PSScriptRoot/items.xml"

# Clear the folder
if (Test-Path -LiteralPath "$PSScriptRoot/$folderName") {
    Remove-Item -Path "$PSScriptRoot/$folderName" -Recurse
}
if (Test-Path $itemsPath) {
    Remove-Item -Path $itemsPath
}
if (Test-Path "$PSScriptRoot/$folderName.epub") {
    Remove-Item -Path "$PSScriptRoot/$folderName.epub"
}

# Create the folder structure
New-Item -Path $PSScriptRoot -Name $folderName -ItemType "directory"
New-Item -Path "$PSScriptRoot/$folderName" -Name "META-INF" -ItemType "directory"
New-Item -Path "$PSScriptRoot/$folderName" -Name "EPUB" -ItemType "directory"
New-Item -Path $basePath -Name "xhtml" -ItemType "directory"
New-Item -Path $basePath -Name "css" -ItemType "directory"
New-Item -Path $basePath -Name "img" -ItemType "directory"

# Create the mimetype file and copy files
Set-Content -Path "$PSScriptRoot/$folderName/mimetype" -Value 'application/epub+zip' 
Copy-Item "$PSScriptRoot/container.xml" -Destination "$PSScriptRoot/$folderName/META-INF"
Copy-Item "$PSScriptRoot/style.css" -Destination "$basePath/css"
Copy-Item "$PSScriptRoot/titlePage.xhtml" -Destination "$basePath/xhtml"

$id = 0
$items = [System.Collections.ArrayList]::new()
for ($i = 2; $i -lt 136; $i++) {
    #135
    $url = "$baseUrl/$i/"
    $htmlDom = ConvertFrom-Html -Url $url
    $jetPosts = $htmlDom.SelectNodes('//div[@class="jet-posts__inner-content"]') | Where-Object {
        $divAuthor = $_.SelectSingleNode('div[@class="jet-title-fields"]/div/div[@class="jet-title-fields__item-value"]')
        $divAuthor.InnerText -eq 'Александр Зубченко'
    }
    
    foreach ($post in $jetPosts) {
        $header = $post.SelectSingleNode('h4[@class="entry-title"]/a');
        $divAuthor = $post.SelectSingleNode('div[@class="jet-title-fields"]/div/div[@class="jet-title-fields__item-value"]')
        $time = $post.SelectSingleNode('div[@class="post-meta"]/span/a/time')
        $arrUrl = $header.Attributes['href'].Value.Split('/')
        $name = $arrUrl[$arrUrl.Length - 2]
        $id++
        $item = [PSCustomObject]@{
            Id        = $id
            Author    = $divAuthor.InnerText
            Title     = $header.InnerText
            Url       = $header.Attributes['href'].Value
            DateTime  = $time.Attributes['datetime'].Value
            FileName  = "$name.xhtml"
            ImageName = ""
            Year      = $time.Attributes['datetime'].Value.Split('-')[0]
            Month     = $time.Attributes['datetime'].Value.Split('-')[1]
        }

        [void]$items.Add($item)

        $itemDom = ConvertFrom-Html -Url $item.Url
        $h1 = $itemDom.SelectSingleNode('//h1[@class="elementor-heading-title elementor-size-default"]')       
        $ps = $itemDom.SelectNodes('//div[@class="elementor-shortcode"]/p')
        $img = $itemDom.SelectSingleNode('//img[@class="main-thumb-text wp-post-image"]')
        $imgName = $item.FileName.Replace('.xhtml', '.jpg')
        if ($null -ne $img) {            
            Invoke-WebRequest $img.Attributes["src"].Value -OutFile "$basePath/img/$imgName"
            $img.Attributes["src"].Value = "../img/$imgName" 
            $item.ImageName = $imgName
        }
                
        $templateDom = ConvertFrom-HTML -Content (Get-Content -Path "$PSScriptRoot/PageTemplate.xhtml" -Raw)
        $title = $templateDom.SelectSingleNode('//title')
        $title.InnerHtml = $item.Title
        $body = $templateDom.SelectSingleNode('//body')
        $refChild = $body.ChildNodes[1];
        $body.InsertBefore($h1, $refChild);
        $date = $item.DateTime.Split('T')[0]
        $dateDiv = $templateDom.SelectSingleNode('//div[@class="post-meta"]')
        $dateDiv.InnerHtml = "Дата публикации: $date <a href='nav.xhtml'>вернуться к содержанию</a>"
        $imgInFirstP = $ps[0].SelectSingleNode("img[@src='../img/$imgName']")
        if ($null -ne $img -and $null -eq $imgInFirstP) {
            $refChild.AppendChild($img)
        }
        
        foreach ($p in $ps) {
            $p.InnerHtml = Clear-Mnemonics -Content $p.InnerHtml
            $refChild.AppendChild($p)
        }
        
        $fileName = $item.FileName
        $outDom = ConvertFrom-HTML -Content $templateDom.OuterHtml
        $outDom.OwnerDocument.OptionOutputAsXml = $true
        $outDom.OwnerDocument.OptionCheckSyntax = $true
        $outDom.OwnerDocument.OptionFixNestedTags = $true    
        $filePath = "$basePath/xhtml/$fileName"    
        $outDom.OwnerDocument.Save($filePath)  
        Update-File -Path $filePath -Url $item.Url
    }    
}

ConvertTo-Xml -InputObject $items -As "String" -NoTypeInformation | Set-Content -Path $itemsPath

$xslt = New-Object System.Xml.Xsl.XslCompiledTransform

$xslt.Load("$PSScriptRoot/nav.xslt")
$xslt.Transform($itemsPath, "$basePath/xhtml/nav.xhtml")

$xslt.Load("$PSScriptRoot/package.xslt")
$xslt.Transform($itemsPath, "$basePath/package.opf")

# Create the epub file
Compress-Archive -Path "$PSScriptRoot/$folderName" -DestinationPath "$PSScriptRoot/$folderName.epub" -CompressionLevel Optimal

# Clear the folder
if (Test-Path -LiteralPath "$PSScriptRoot/$folderName") {
    Remove-Item -Path "$PSScriptRoot/$folderName" -Recurse
}
if (Test-Path $itemsPath) {
    Remove-Item -Path $itemsPath
}