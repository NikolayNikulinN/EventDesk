param(
    [Parameter(Mandatory = $true)][string]$ResourceFile,
    [Parameter(Mandatory = $true)][string]$OutputFile
)

$ErrorActionPreference = 'Stop'

[xml]$resourceDocument = Get-Content -LiteralPath $ResourceFile -Raw
$installerStrings = @($resourceDocument.root.data | Where-Object {
    $_.GetAttribute('name').StartsWith('Installer', [StringComparison]::Ordinal)
})

if ($installerStrings.Count -eq 0) {
    throw 'No installer strings were found in AppResource.resx.'
}

$document = New-Object System.Xml.XmlDocument
$null = $document.AppendChild($document.CreateXmlDeclaration('1.0', 'utf-8', $null))
$root = $document.CreateElement('WixLocalization', 'http://wixtoolset.org/schemas/v4/wxl')
$root.SetAttribute('Culture', 'en-US')
$root.SetAttribute('Language', '1033')
$null = $document.AppendChild($root)

foreach ($entry in $installerStrings) {
    $string = $document.CreateElement('String', 'http://wixtoolset.org/schemas/v4/wxl')
    $string.SetAttribute('Id', $entry.GetAttribute('name').Substring('Installer'.Length))
    $string.SetAttribute('Value', $entry.SelectSingleNode('value').InnerText)
    $null = $root.AppendChild($string)
}

$directory = Split-Path -Parent $OutputFile
New-Item -ItemType Directory -Path $directory -Force | Out-Null
$document.Save($OutputFile)
