# $transform = "$PSScriptRoot/package.xslt"
# $output = "$PSScriptRoot/package.opf"
$transform = "$PSScriptRoot/nav.xslt"
$output = "$PSScriptRoot/nav.xhtml"

$xslt = New-Object System.Xml.Xsl.XslCompiledTransform;
$xslt.Load($transform);
$xslt.Transform("$PSScriptRoot/items.xml", $output);