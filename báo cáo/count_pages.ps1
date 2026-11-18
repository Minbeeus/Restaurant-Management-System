$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$docPath = Join-Path $scriptDir "Bao_Cao_Do_An_Tot_Nghiep_MoDau_KetLuan.docx"
$word = New-Object -ComObject Word.Application
$word.Visible = $false
$doc = $word.Documents.Open($docPath)
$pages = $doc.ComputeStatistics(2)
$words = $doc.ComputeStatistics(0)
$doc.Close()
$word.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($word) | Out-Null
Write-Host "Page Count: $pages"
Write-Host "Word Count: $words"
