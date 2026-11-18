$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourcePath = Join-Path $scriptDir "Bao_Cao_Do_An_Tot_Nghiep_MoDau_KetLuan.docx"
$tempPath = Join-Path $scriptDir "temp_count.docx"

Copy-Item $sourcePath $tempPath -Force

$word = New-Object -ComObject Word.Application
$word.Visible = $false
try {
    $doc = $word.Documents.Open($tempPath, $false, $true) # ReadOnly
    $pages = $doc.ComputeStatistics(2)
    $words = $doc.ComputeStatistics(0)
    $doc.Close()
    Write-Host "PAGES_RESULT:$pages"
    Write-Host "WORDS_RESULT:$words"
} finally {
    $word.Quit()
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($word) | Out-Null
    Remove-Item $tempPath -Force -ErrorAction SilentlyContinue
}
