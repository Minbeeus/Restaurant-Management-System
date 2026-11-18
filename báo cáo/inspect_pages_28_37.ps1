$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$docPath = Join-Path $scriptDir "Bao_Cao_Do_An_Tot_Nghiep_MoDau_KetLuan.docx"
$word = New-Object -ComObject Word.Application
$word.Visible = $false
$doc = $word.Documents.Open($docPath)

Write-Host "=== INSPECTING PAGES 28 TO 37 ==="
for ($p = 28; $p -le 37; $p++) {
    Write-Host "-------------------- PAGE $p --------------------"
    # Find text on page $p using GoTo wdGoToPage
    $pageRange = $doc.GoTo(1, 1, $p) # wdGoToPage, wdGoToAbsolute, pageNumber
    $pageEnd = $doc.GoTo(1, 1, $p + 1)
    
    $range = $doc.Range($pageRange.Start, $pageEnd.Start)
    $text = $range.Text
    $tablesCount = $range.Tables.Count
    $shapesCount = $range.InlineShapes.Count
    Write-Host "Tables on page: $tablesCount | Images on page: $shapesCount"
    # Print first few lines and any figure/table titles
    $lines = $text -split "`r`n|`r|`n" | Where-Object { $_.Trim().Length -gt 0 }
    foreach ($l in $lines) {
        if ($l -match "Hình" -or $l -match "Bảng" -or $l -match "^\d\." -or $l -match "^CHƯƠNG") {
            Write-Host "   [HEADING/OBJECT] $l"
        }
    }
    if ($lines.Count -gt 0) {
        Write-Host "   [First line]: $($lines[0].Substring(0, [Math]::Min(80, $lines[0].Length)))"
    }
}

$doc.Close()
$word.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($word) | Out-Null
