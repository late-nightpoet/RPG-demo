$ErrorActionPreference = "Stop"

Set-StrictMode -Version Latest

function New-Color([string]$hex) {
    $hex = $hex.Trim().TrimStart("#")
    if ($hex.Length -ne 6) { throw "Invalid hex color: $hex" }
    $r = [Convert]::ToInt32($hex.Substring(0, 2), 16)
    $g = [Convert]::ToInt32($hex.Substring(2, 2), 16)
    $b = [Convert]::ToInt32($hex.Substring(4, 2), 16)
    return [System.Drawing.Color]::FromArgb(255, $r, $g, $b)
}

function New-Font([string]$preferred, [float]$size, [System.Drawing.FontStyle]$style = [System.Drawing.FontStyle]::Regular) {
    try {
        return New-Object System.Drawing.Font($preferred, $size, $style)
    } catch {
        # Fallback for machines without the preferred font.
        return New-Object System.Drawing.Font("Segoe UI", $size, $style)
    }
}

function Draw-RoundedRect([System.Drawing.Graphics]$g, [System.Drawing.Pen]$pen, [System.Drawing.Brush]$brush, [int]$x, [int]$y, [int]$w, [int]$h, [int]$r) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $path.AddArc($x, $y, $d, $d, 180, 90)
    $path.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $path.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $path.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $path.CloseFigure() | Out-Null
    $g.FillPath($brush, $path)
    $g.DrawPath($pen, $path)
    $path.Dispose()
}

function Draw-Box([System.Drawing.Graphics]$g, $node) {
    $x = [int]$node.X; $y = [int]$node.Y; $w = [int]$node.W; $h = [int]$node.H
    $radius = if ($node.Radius) { [int]$node.Radius } else { 12 }

    $fill = New-Object System.Drawing.SolidBrush($node.Fill)
    $borderPen = New-Object System.Drawing.Pen($node.Border, 2)

    Draw-RoundedRect $g $borderPen $fill $x $y $w $h $radius

    $pad = 14
    $titleFont = $node.TitleFont
    $bodyFont = $node.BodyFont
    $titleBrush = New-Object System.Drawing.SolidBrush($node.TitleColor)
    $bodyBrush = New-Object System.Drawing.SolidBrush($node.BodyColor)

    $titleY = $y + $pad
    $g.DrawString($node.Title, $titleFont, $titleBrush, $x + $pad, $titleY)

    if ($node.Lines -and $node.Lines.Count -gt 0) {
        $lineY = $titleY + 32
        foreach ($line in $node.Lines) {
            $g.DrawString($line, $bodyFont, $bodyBrush, $x + $pad, $lineY)
            $lineY += 22
        }
    }

    $fill.Dispose()
    $borderPen.Dispose()
    $titleBrush.Dispose()
    $bodyBrush.Dispose()
}

function Get-Anchor($node, [ValidateSet("L","R","T","B","C")] [string]$side) {
    $x = [double]$node.X; $y = [double]$node.Y; $w = [double]$node.W; $h = [double]$node.H
    switch ($side) {
        "L" { return [PSCustomObject]@{ X = $x;       Y = $y + $h / 2 } }
        "R" { return [PSCustomObject]@{ X = $x + $w;  Y = $y + $h / 2 } }
        "T" { return [PSCustomObject]@{ X = $x + $w / 2; Y = $y } }
        "B" { return [PSCustomObject]@{ X = $x + $w / 2; Y = $y + $h } }
        "C" { return [PSCustomObject]@{ X = $x + $w / 2; Y = $y + $h / 2 } }
    }
}

function Draw-Arrow([System.Drawing.Graphics]$g, $from, $to, [string]$style, [string]$label = $null) {
    $pen = New-Object System.Drawing.Pen((New-Color "2D3748"), 2)
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    if ($style -eq "dashed") { $pen.DashStyle = [System.Drawing.Drawing2D.DashStyle]::Dash }

    $p1 = New-Object System.Drawing.PointF([float]$from.X, [float]$from.Y)
    $p2 = New-Object System.Drawing.PointF([float]$to.X, [float]$to.Y)
    $g.DrawLine($pen, $p1, $p2)

    # Arrow head (open triangle for inheritance, filled for others)
    $dx = [double]($to.X - $from.X)
    $dy = [double]($to.Y - $from.Y)
    $len = [Math]::Sqrt($dx * $dx + $dy * $dy)
    if ($len -gt 0.001) {
        $ux = $dx / $len
        $uy = $dy / $len
        $size = 12.0
        $baseX = $to.X - $ux * $size
        $baseY = $to.Y - $uy * $size
        $px = -$uy
        $py = $ux
        $left = New-Object System.Drawing.PointF([float]($baseX + $px * ($size * 0.55)), [float]($baseY + $py * ($size * 0.55)))
        $right = New-Object System.Drawing.PointF([float]($baseX - $px * ($size * 0.55)), [float]($baseY - $py * ($size * 0.55)))
        $tip = New-Object System.Drawing.PointF([float]$to.X, [float]$to.Y)

        if ($style -eq "inherit") {
            $headPen = New-Object System.Drawing.Pen((New-Color "2D3748"), 2)
            $g.DrawPolygon($headPen, @($tip, $left, $right))
            $headPen.Dispose()
        } else {
            $brush = [System.Drawing.Brushes]::Black
            $g.FillPolygon($brush, @($tip, $left, $right))
        }
    }

    if ($label) {
        $font = New-Font "Segoe UI" 11 ([System.Drawing.FontStyle]::Regular)
        $brush = New-Object System.Drawing.SolidBrush((New-Color "4A5568"))
        $mx = [float](($from.X + $to.X) / 2.0 + 6)
        $my = [float](($from.Y + $to.Y) / 2.0 + 6)
        $g.DrawString($label, $font, $brush, $mx, $my)
        $brush.Dispose()
        $font.Dispose()
    }

    $pen.Dispose()
}

function New-Node([string]$id, [int]$x, [int]$y, [int]$w, [int]$h, [string]$title, [string[]]$lines, [string]$fillHex) {
    return [PSCustomObject]@{
        Id = $id
        X = $x; Y = $y; W = $w; H = $h
        Title = $title
        Lines = $lines
        Fill = (New-Color $fillHex)
        Border = (New-Color "1A202C")
        TitleColor = (New-Color "1A202C")
        BodyColor = (New-Color "2D3748")
        TitleFont = (New-Font "Microsoft YaHei UI" 14 ([System.Drawing.FontStyle]::Bold))
        BodyFont = (New-Font "Microsoft YaHei UI" 11 ([System.Drawing.FontStyle]::Regular))
        Radius = 12
    }
}

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

$repoRoot = Split-Path -Parent $PSScriptRoot
$outDir = Join-Path $repoRoot "docs\\diagrams\\png"
New-Item -ItemType Directory -Force $outDir | Out-Null

function Export-ArchitecturePng {
    $width = 1920; $height = 1080
    $bmp = New-Object System.Drawing.Bitmap $width, $height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit
    $g.Clear((New-Color "F7FAFC"))

    $titleFont = New-Font "Microsoft YaHei UI" 22 ([System.Drawing.FontStyle]::Bold)
    $titleBrush = New-Object System.Drawing.SolidBrush((New-Color "1A202C"))
    $g.DrawString("Architecture Overview", $titleFont, $titleBrush, 40, 24)
    $titleBrush.Dispose()
    $titleFont.Dispose()

    $nodes = @{}
    $nodes.InputAsset = New-Node "InputAsset" 60 110 480 120 "PlayerInput.inputactions" @("Input System bindings") "E6FFFA"
    $nodes.InputReader = New-Node "InputReader" 60 250 480 140 "PlayerInputReader" @("Reads Move/Look/Sprint/Roll", "Raises input events") "E6FFFA"

    $nodes.PlayerCtrl = New-Node "PlayerCtrl" 600 110 560 160 "Player_Controller" @("Inherits CharacterBase", "Assembles PlayerBlackBoard") "EBF4FF"
    $nodes.PlayerMove = New-Node "PlayerMove" 600 300 560 160 "PlayerMovementHelper" @("Camera-relative movement", "Gait + gravity + grounded") "EBF4FF"
    $nodes.PlayerSM = New-Node "PlayerSM" 600 490 560 150 "StateMachine (Player)" @("Pure C# states", "Update via MonoManager") "EBF4FF"

    $nodes.BossCtrl = New-Node "BossCtrl" 1240 110 620 160 "BOSS_Controller" @("Inherits CharacterBase", "NavMesh + AI state") "FFF5F5"
    $nodes.NavMesh = New-Node "NavMesh" 1240 300 620 160 "NavMeshAgent" @("Chase / vigilant standoff") "FFF5F5"
    $nodes.BossSM = New-Node "BossSM" 1240 490 620 150 "StateMachine (Boss)" @("Attack decision", "Hurt chain") "FFF5F5"

    $nodes.CharacterBase = New-Node "CharacterBase" 200 740 520 170 "CharacterBase" @("StartAttack / OnHit / Hurt", "FreezeFrame + TimeScale", "HP + audio + VFX spawn") "F0FFF4"
    $nodes.ModelBase = New-Node "ModelBase" 760 740 520 170 "ModelBase" @("Animator + RootMotion bridge", "AnimationEvents -> weapons") "F0FFF4"
    $nodes.Weapon = New-Node "Weapon" 1320 740 540 170 "Weapon_Controller" @("Trigger hit detect + de-dup", "Invokes ISkillOwner.OnHit") "F0FFF4"

    foreach ($k in $nodes.Keys) { Draw-Box $g $nodes[$k] }

    Draw-Arrow $g (Get-Anchor $nodes.InputAsset "B") (Get-Anchor $nodes.InputReader "T") "solid"
    Draw-Arrow $g (Get-Anchor $nodes.InputReader "R") (Get-Anchor $nodes.PlayerMove "L") "solid"
    Draw-Arrow $g (Get-Anchor $nodes.PlayerMove "T") (Get-Anchor $nodes.PlayerCtrl "B") "solid"
    Draw-Arrow $g (Get-Anchor $nodes.PlayerCtrl "B") (Get-Anchor $nodes.PlayerSM "T") "solid" "ChangeState<T>()"

    Draw-Arrow $g (Get-Anchor $nodes.BossCtrl "B") (Get-Anchor $nodes.NavMesh "T") "solid"
    Draw-Arrow $g (Get-Anchor $nodes.NavMesh "B") (Get-Anchor $nodes.BossSM "T") "solid"

    Draw-Arrow $g (Get-Anchor $nodes.PlayerCtrl "B") (Get-Anchor $nodes.CharacterBase "T") "dashed" "uses"
    Draw-Arrow $g (Get-Anchor $nodes.BossCtrl "B") (Get-Anchor $nodes.CharacterBase "T") "dashed" "uses"

    Draw-Arrow $g (Get-Anchor $nodes.CharacterBase "R") (Get-Anchor $nodes.ModelBase "L") "solid"
    Draw-Arrow $g (Get-Anchor $nodes.ModelBase "R") (Get-Anchor $nodes.Weapon "L") "solid"

    $legendFont = New-Font "Segoe UI" 11 ([System.Drawing.FontStyle]::Regular)
    $legendBrush = New-Object System.Drawing.SolidBrush((New-Color "4A5568"))
    $g.DrawString("Legend: solid = data/control flow, dashed = uses", $legendFont, $legendBrush, 40, 1040)
    $legendBrush.Dispose()
    $legendFont.Dispose()

    $outPath = Join-Path $outDir "architecture.png"
    $bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
}

function Export-UmlClassPng {
    $width = 2400; $height = 1400
    $bmp = New-Object System.Drawing.Bitmap $width, $height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit
    $g.Clear((New-Color "FFFBF5"))

    $titleFont = New-Font "Microsoft YaHei UI" 22 ([System.Drawing.FontStyle]::Bold)
    $titleBrush = New-Object System.Drawing.SolidBrush((New-Color "1A202C"))
    $g.DrawString("UML (Key Classes)", $titleFont, $titleBrush, 40, 24)
    $titleBrush.Dispose()
    $titleFont.Dispose()

    $n = @{}
    $n.IStateMachineOwner = New-Node "IStateMachineOwner" 60 110 420 120 "IStateMachineOwner" @("<<interface>>") "FFF5F5"
    $n.ISkillOwner = New-Node "ISkillOwner" 60 250 420 120 "ISkillOwner" @("<<interface>>") "FFF5F5"
    $n.IHurt = New-Node "IHurt" 60 390 420 120 "IHurt" @("<<interface>>") "FFF5F5"

    $n.StateBase = New-Node "StateBase" 540 110 460 180 "StateBase" @("+Enter/Exit", "+Update/Fixed/Late") "EDF2F7"
    $n.StateMachine = New-Node "StateMachine" 540 320 460 170 "StateMachine" @("+ChangeState<T>()", "+Stop()") "EDF2F7"
    $n.MonoManager = New-Node "MonoManager" 540 520 460 160 "MonoManager" @("Update dispatcher", "SingletonMono<T>") "EDF2F7"

    $n.CharacterBase = New-Node "CharacterBase" 1060 180 560 220 "CharacterBase" @("MonoBehaviour", "StartAttack / OnHit / Hurt*", "holds: ModelBase + StateMachine") "E6FFFA"
    $n.ModelBase = New-Node "ModelBase" 1060 450 560 200 "ModelBase" @("Animator", "OnAnimatorMove -> RootMotionAction", "AnimationEvents -> Weapon") "E6FFFA"
    $n.Weapon = New-Node "Weapon_Controller" 1060 700 560 190 "Weapon_Controller" @("StartSkillHit/StopSkillHit", "OnTriggerStay -> OnHit") "E6FFFA"

    $n.PlayerController = New-Node "Player_Controller" 1680 160 520 160 "Player_Controller" @("<|-- CharacterBase") "EBF4FF"
    $n.BossController = New-Node "BOSS_Controller" 1680 340 520 160 "BOSS_Controller" @("<|-- CharacterBase") "EBF4FF"

    $n.PlayerStateBase = New-Node "PlayerStateBase" 1680 540 520 160 "PlayerStateBase" @("<|-- StateBase") "F0FFF4"
    $n.BossStateBase = New-Node "BossStateBase" 1680 720 520 160 "BossStateBase" @("<|-- StateBase") "F0FFF4"

    $n.SkillConfig = New-Node "SkillConfig" 60 930 560 190 "SkillConfig" @("<<ScriptableObject>>", "AnimationName", "ReleaseData + AttackData[]") "FEFCBF"
    $n.SkillAttackData = New-Node "Skill_AttackData" 660 930 560 190 "Skill_AttackData" @("HitData", "VFX/Audio", "FreezeFrame/TimeStop") "FEFCBF"
    $n.SkillHitData = New-Node "Skill_HitData" 1260 930 560 190 "Skill_HitData" @("Damage/Break", "IsKnockUp", "RepelVelocity/Time") "FEFCBF"
    $n.SkillHitEFConfig = New-Node "SkillHitEFConfig" 1860 930 480 190 "SkillHitEFConfig" @("<<ScriptableObject>>", "SpawnObject/FailSpawnObject") "FEFCBF"

    foreach ($k in $n.Keys) { Draw-Box $g $n[$k] }

    # Interfaces implemented by CharacterBase
    Draw-Arrow $g (Get-Anchor $n.IStateMachineOwner "R") (Get-Anchor $n.CharacterBase "L") "dashed" "implements"
    Draw-Arrow $g (Get-Anchor $n.ISkillOwner "R") (Get-Anchor $n.CharacterBase "L") "dashed" "implements"
    Draw-Arrow $g (Get-Anchor $n.IHurt "R") (Get-Anchor $n.CharacterBase "L") "dashed" "implements"

    # State machine relationships
    Draw-Arrow $g (Get-Anchor $n.StateMachine "T") (Get-Anchor $n.StateBase "B") "solid" "has StateBase"
    Draw-Arrow $g (Get-Anchor $n.MonoManager "T") (Get-Anchor $n.StateMachine "B") "dashed" "dispatches"

    # CharacterBase composition
    Draw-Arrow $g (Get-Anchor $n.CharacterBase "B") (Get-Anchor $n.ModelBase "T") "solid" "has"
    Draw-Arrow $g (Get-Anchor $n.ModelBase "B") (Get-Anchor $n.Weapon "T") "solid" "has"
    Draw-Arrow $g (Get-Anchor $n.CharacterBase "L") (Get-Anchor $n.StateMachine "R") "solid" "has"

    # Inheritance
    Draw-Arrow $g (Get-Anchor $n.PlayerController "L") (Get-Anchor $n.CharacterBase "R") "inherit"
    Draw-Arrow $g (Get-Anchor $n.BossController "L") (Get-Anchor $n.CharacterBase "R") "inherit"
    Draw-Arrow $g (Get-Anchor $n.PlayerStateBase "L") (Get-Anchor $n.StateBase "R") "inherit"
    Draw-Arrow $g (Get-Anchor $n.BossStateBase "L") (Get-Anchor $n.StateBase "R") "inherit"

    # Skill data graph
    Draw-Arrow $g (Get-Anchor $n.SkillConfig "R") (Get-Anchor $n.SkillAttackData "L") "solid" "AttackData[]"
    Draw-Arrow $g (Get-Anchor $n.SkillAttackData "R") (Get-Anchor $n.SkillHitData "L") "solid" "HitData"
    Draw-Arrow $g (Get-Anchor $n.SkillAttackData "B") (Get-Anchor $n.SkillHitEFConfig "B") "dashed" "uses"

    $legendFont = New-Font "Segoe UI" 11 ([System.Drawing.FontStyle]::Regular)
    $legendBrush = New-Object System.Drawing.SolidBrush((New-Color "4A5568"))
    $g.DrawString("Legend: inherit = open triangle arrow, dashed = uses/implements", $legendFont, $legendBrush, 40, 1348)
    $legendBrush.Dispose()
    $legendFont.Dispose()

    $outPath = Join-Path $outDir "uml-class.png"
    $bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
}

Export-ArchitecturePng
Export-UmlClassPng

Write-Host "Wrote:"
Write-Host ("- " + (Join-Path $outDir "architecture.png"))
Write-Host ("- " + (Join-Path $outDir "uml-class.png"))

