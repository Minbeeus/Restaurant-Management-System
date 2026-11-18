param([string]$fileName = "test_page_calc.docx")
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$docPath = Join-Path $scriptDir $fileName
$word = New-Object -ComObject Word.Application
$word.Visible = $false
$doc = $word.Documents.Open($docPath)
$pages = $doc.ComputeStatistics(2)
$words = $doc.ComputeStatistics(0)
$doc.Close()
$word.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($word) | Out-Null
Write-Host "File: $fileName -> Pages: $pages, Words: $words"
