Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Net.Http

[System.Windows.Forms.Application]::EnableVisualStyles()
[System.Net.ServicePointManager]::DefaultConnectionLimit = 10

# =========================================================
# Win32 P/Invoke
# =========================================================
if (-not ('Win32Native' -as [type])) {
    Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public static class Win32Native {
    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll", SetLastError = true)]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
"@
}

# =========================================================
# RoundButton
# =========================================================
if (-not ('RoundButton' -as [type])) {
    Add-Type -ReferencedAssemblies System.Windows.Forms, System.Drawing -TypeDefinition @"
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
public class RoundButton : Button {
    public int CornerRadius;
    public Color NormalBack;
    public Color HoverBack;
    public Color PressBack;
    public Color BorderColor;
    private bool hover;
    private bool press;
    public RoundButton() {
        CornerRadius = 8;
        NormalBack  = Color.FromArgb(0,120,212);
        HoverBack   = Color.FromArgb(0,140,232);
        PressBack   = Color.FromArgb(0,100,180);
        BorderColor = Color.Empty;
        hover = false;
        press = false;
        this.FlatStyle = FlatStyle.Flat;
        this.FlatAppearance.BorderSize = 0;
        this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                      ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }
    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; press = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { press = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { press = false; Invalidate(); base.OnMouseUp(e); }
    private GraphicsPath GetPath(Rectangle r, int radius) {
        int d = radius * 2;
        var p = new GraphicsPath();
        if (d <= 0) { p.AddRectangle(r); return p; }
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
    protected override void OnPaint(PaintEventArgs e) {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(this.Parent != null ? this.Parent.BackColor : Color.FromArgb(32,32,32));
        var back = press ? PressBack : (hover ? HoverBack : NormalBack);
        using (var path = GetPath(new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius)) {
            using (var b = new SolidBrush(back)) g.FillPath(b, path);
            if (BorderColor != Color.Empty) {
                using (var p = new Pen(BorderColor, 1)) g.DrawPath(p, path);
            }
        }
        TextRenderer.DrawText(g, Text, Font, new Rectangle(0, 0, Width, Height), ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}
"@
}

# =========================================================
# FlatTabControl
# =========================================================
if (-not ('FlatTabControl' -as [type])) {
    Add-Type -ReferencedAssemblies System.Windows.Forms, System.Drawing -TypeDefinition @"
using System;
using System.Drawing;
using System.Windows.Forms;
public class FlatTabControl : TabControl {
    public Color TabBackColor;
    public Color TabActiveBackColor;
    public Color TabTextColor;
    public Color TabActiveTextColor;
    public Color AccentColor;
    public Font ActiveFont;
    public Font InactiveFont;
    public FlatTabControl() {
        TabBackColor = Color.FromArgb(32,32,32);
        TabActiveBackColor = Color.FromArgb(43,43,43);
        TabTextColor = Color.FromArgb(180,180,180);
        TabActiveTextColor = Color.FromArgb(240,240,240);
        AccentColor = Color.FromArgb(0,120,212);
        ActiveFont = new Font("Segoe UI", 10, FontStyle.Bold);
        InactiveFont = new Font("Segoe UI", 10, FontStyle.Regular);
        this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                      ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        this.DrawMode = TabDrawMode.OwnerDrawFixed;
        this.SizeMode = TabSizeMode.Fixed;
        this.ItemSize = new Size(140, 36);
        this.Padding = new Point(16, 4);
    }
    protected override void OnPaint(PaintEventArgs e) {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using (var b = new SolidBrush(TabBackColor)) {
            g.FillRectangle(b, 0, 0, this.Width, this.ItemSize.Height + 4);
        }

        for (int i = 0; i < this.TabCount; i++) {
            Rectangle r = this.GetTabRect(i);
            bool isSel = (this.SelectedIndex == i);

            if (isSel) {
                int rad = 6;
                int d = rad * 2;
                using (var path = new System.Drawing.Drawing2D.GraphicsPath()) {
                    path.AddArc(r.X, r.Y + 2, d, d, 180, 90);
                    path.AddArc(r.Right - d, r.Y + 2, d, d, 270, 90);
                    path.AddLine(r.Right, r.Bottom - 2, r.X, r.Bottom - 2);
                    path.CloseFigure();
                    using (var b = new SolidBrush(TabActiveBackColor)) g.FillPath(b, path);
                    using (var p = new Pen(AccentColor, 2)) {
                        g.DrawLine(p, r.X + 6, r.Bottom - 1, r.Right - 6, r.Bottom - 1);
                    }
                }
            }

            string text = this.TabPages[i].Text;
            Color c = isSel ? TabActiveTextColor : TabTextColor;
            Font f = isSel ? ActiveFont : InactiveFont;
            var sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            sf.LineAlignment = StringAlignment.Center;
            var rf = new RectangleF(r.X, r.Y, r.Width, r.Height - 2);
            using (var b = new SolidBrush(c)) g.DrawString(text, f, b, rf, sf);
        }
    }
    protected override void OnPaintBackground(PaintEventArgs e) {
        e.Graphics.Clear(TabBackColor);
    }
}
"@
}

# =========================================================
# Paths and logging
# =========================================================
$scriptDir     = Split-Path -Parent $MyInvocation.MyCommand.Path
$favPath       = Join-Path $scriptDir 'favorites.json'
$countriesPath = Join-Path $scriptDir 'countries.json'
$tagsPath      = Join-Path $scriptDir 'tags.json'
$logPath       = Join-Path $scriptDir 'radioBrowser.log'
$imgCacheDir   = Join-Path $env:TEMP 'RadioBrowserCache'
if (-not (Test-Path $imgCacheDir)) { New-Item -ItemType Directory -Path $imgCacheDir | Out-Null }

if (Test-Path $logPath) {
    try { if ((Get-Item $logPath).Length -gt 5MB) { Move-Item $logPath "$logPath.old" -Force } } catch { }
}

function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    $ts = Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff'
    $line = "[$ts] [$Level] $Message"
    try {
        [System.IO.File]::AppendAllText($logPath, $line + [Environment]::NewLine,
            (New-Object System.Text.UTF8Encoding($false)))
    } catch { }
    $color = switch ($Level) {
        'FATAL' { 'Magenta' } 'ERROR' { 'Red' } 'WARN' { 'Yellow' }
        'DEBUG' { 'DarkGray' } default { 'Gray' }
    }
    Write-Host $line -ForegroundColor $color
}

Write-Log "=== RadioBrowser started ==="

[System.Windows.Forms.Application]::add_ThreadException({
    param($sender, $e)
    Write-Log "ThreadException: $($e.Exception.Message)`n$($e.Exception.StackTrace)" 'FATAL'
})
[System.AppDomain]::CurrentDomain.add_UnhandledException({
    param($sender, $e)
    Write-Log "UnhandledException: $($e.ExceptionObject.ToString())" 'FATAL'
})

# =========================================================
# State
# =========================================================
$script:allStations    = New-Object System.Collections.ArrayList
$script:favStations    = New-Object System.Collections.ArrayList
$script:currentStation = $null
$script:nowPlaying     = $null
$script:imgCache       = @{}
$script:flagCache      = @{}
$script:loggedMeta     = $false
$script:selectedCard   = $null

$script:CardWidth  = 370
$script:CardHeight = 76
$script:CardGapX   = 6
$script:CardGapY   = 4

# Paging state
$script:searchLimit    = 20
$script:searchOffset   = 0
$script:searchQuery    = ''
$script:searchHasMore  = $true
$script:searchInFlight = $false

# Async load state
$script:asyncPending = $null

# ICY state
$script:icyInFlight       = $false
$script:icyTickCounter    = 0
$script:icyKickRestart    = $false
$script:icyInitialDelay   = 0
$script:icyInitialDue     = $false

$script:searchCards   = New-Object System.Collections.ArrayList
$script:favCards      = New-Object System.Collections.ArrayList

$script:iconQueue = New-Object System.Collections.Queue

$handler = New-Object System.Net.Http.HttpClientHandler
$handler.AllowAutoRedirect = $true
$handler.MaxAutomaticRedirections = 5
$script:httpClient = New-Object System.Net.Http.HttpClient($handler)
$script:httpClient.Timeout = [TimeSpan]::FromSeconds(8)
try {
    $script:httpClient.DefaultRequestHeaders.UserAgent.ParseAdd('Mozilla/5.0 RadioBrowser/1.0')
} catch { }

# =========================================================
# Palette
# =========================================================
$script:ClrBg        = [System.Drawing.Color]::FromArgb(32, 32, 32)
$script:ClrCardBg    = [System.Drawing.Color]::FromArgb(43, 43, 43)
$script:ClrCardHover = [System.Drawing.Color]::FromArgb(56, 56, 56)
$script:ClrCardSel   = [System.Drawing.Color]::FromArgb(0, 120, 212)
$script:ClrAccent    = [System.Drawing.Color]::FromArgb(0, 120, 212)
$script:ClrBorder    = [System.Drawing.Color]::FromArgb(70, 70, 70)
$script:ClrText      = [System.Drawing.Color]::FromArgb(240, 240, 240)
$script:ClrTextDim   = [System.Drawing.Color]::FromArgb(180, 180, 180)
$script:ClrStar      = [System.Drawing.Color]::FromArgb(255, 200, 60)
$script:FontTitle    = New-Object System.Drawing.Font('Segoe UI', 10, [System.Drawing.FontStyle]::Bold)
$script:FontMeta     = New-Object System.Drawing.Font('Segoe UI', 8.5)
$script:FontTags     = New-Object System.Drawing.Font('Segoe UI', 8)
$script:FontTab      = New-Object System.Drawing.Font('Segoe UI', 10)
$script:FontTabBold  = New-Object System.Drawing.Font('Segoe UI', 10, [System.Drawing.FontStyle]::Bold)

$script:WMPS_PLAYING = 3

# =========================================================
# WMP
# =========================================================
$wmp = New-Object -ComObject WMPlayer.OCX
$wmp.settings.volume    = 50
$wmp.settings.autoStart = $true

# =========================================================
# Country names
# =========================================================
$script:CountryNames = @{
    'AD'='Andorra';'AE'='United Arab Emirates';'AF'='Afghanistan';'AG'='Antigua and Barbuda';
    'AI'='Anguilla';'AL'='Albania';'AM'='Armenia';'AO'='Angola';'AQ'='Antarctica';
    'AR'='Argentina';'AS'='American Samoa';'AT'='Austria';'AU'='Australia';'AW'='Aruba';
    'AX'='Aland Islands';'AZ'='Azerbaijan';'BA'='Bosnia and Herzegovina';'BB'='Barbados';
    'BD'='Bangladesh';'BE'='Belgium';'BF'='Burkina Faso';'BG'='Bulgaria';'BH'='Bahrain';
    'BI'='Burundi';'BJ'='Benin';'BL'='Saint Barthelemy';'BM'='Bermuda';'BN'='Brunei';
    'BO'='Bolivia';'BQ'='Bonaire';'BR'='Brazil';'BS'='Bahamas';'BT'='Bhutan';
    'BV'='Bouvet Island';'BW'='Botswana';'BY'='Belarus';'BZ'='Belize';'CA'='Canada';
    'CC'='Cocos Islands';'CD'='DR Congo';'CF'='Central African Republic';'CG'='Congo';
    'CH'='Switzerland';'CI'='Ivory Coast';'CK'='Cook Islands';'CL'='Chile';'CM'='Cameroon';
    'CN'='China';'CO'='Colombia';'CR'='Costa Rica';'CU'='Cuba';'CV'='Cape Verde';
    'CW'='Curacao';'CX'='Christmas Island';'CY'='Cyprus';'CZ'='Czechia';'DE'='Germany';
    'DJ'='Djibouti';'DK'='Denmark';'DM'='Dominica';'DO'='Dominican Republic';'DZ'='Algeria';
    'EC'='Ecuador';'EE'='Estonia';'EG'='Egypt';'EH'='Western Sahara';'ER'='Eritrea';
    'ES'='Spain';'ET'='Ethiopia';'FI'='Finland';'FJ'='Fiji';'FK'='Falkland Islands';
    'FM'='Micronesia';'FO'='Faroe Islands';'FR'='France';'GA'='Gabon';'GB'='United Kingdom';
    'GD'='Grenada';'GE'='Georgia';'GF'='French Guiana';'GG'='Guernsey';'GH'='Ghana';
    'GI'='Gibraltar';'GL'='Greenland';'GM'='Gambia';'GN'='Guinea';'GP'='Guadeloupe';
    'GQ'='Equatorial Guinea';'GR'='Greece';'GS'='South Georgia';'GT'='Guatemala';'GU'='Guam';
    'GW'='Guinea-Bissau';'GY'='Guyana';'HK'='Hong Kong';'HM'='Heard Island';'HN'='Honduras';
    'HR'='Croatia';'HT'='Haiti';'HU'='Hungary';'ID'='Indonesia';'IE'='Ireland';
    'IL'='Israel';'IM'='Isle of Man';'IN'='India';'IO'='British Indian Ocean Territory';
    'IQ'='Iraq';'IR'='Iran';'IS'='Iceland';'IT'='Italy';'JE'='Jersey';'JM'='Jamaica';
    'JO'='Jordan';'JP'='Japan';'KE'='Kenya';'KG'='Kyrgyzstan';'KH'='Cambodia';
    'KI'='Kiribati';'KM'='Comoros';'KN'='Saint Kitts and Nevis';'KP'='North Korea';
    'KR'='South Korea';'KW'='Kuwait';'KY'='Cayman Islands';'KZ'='Kazakhstan';'LA'='Laos';
    'LB'='Lebanon';'LC'='Saint Lucia';'LI'='Liechtenstein';'LK'='Sri Lanka';'LR'='Liberia';
    'LS'='Lesotho';'LT'='Lithuania';'LU'='Luxembourg';'LV'='Latvia';'LY'='Libya';
    'MA'='Morocco';'MC'='Monaco';'MD'='Moldova';'ME'='Montenegro';'MF'='Saint Martin';
    'MG'='Madagascar';'MH'='Marshall Islands';'MK'='North Macedonia';'ML'='Mali';
    'MM'='Myanmar';'MN'='Mongolia';'MO'='Macao';'MP'='Northern Mariana Islands';
    'MQ'='Martinique';'MR'='Mauritania';'MS'='Montserrat';'MT'='Malta';'MU'='Mauritius';
    'MV'='Maldives';'MW'='Malawi';'MX'='Mexico';'MY'='Malaysia';'MZ'='Mozambique';
    'NA'='Namibia';'NC'='New Caledonia';'NE'='Niger';'NF'='Norfolk Island';'NG'='Nigeria';
    'NI'='Nicaragua';'NL'='Netherlands';'NO'='Norway';'NP'='Nepal';'NR'='Nauru';
    'NU'='Niue';'NZ'='New Zealand';'OM'='Oman';'PA'='Panama';'PE'='Peru';
    'PF'='French Polynesia';'PG'='Papua New Guinea';'PH'='Philippines';'PK'='Pakistan';
    'PL'='Poland';'PM'='Saint Pierre and Miquelon';'PN'='Pitcairn';'PR'='Puerto Rico';
    'PS'='Palestine';'PT'='Portugal';'PW'='Palau';'PY'='Paraguay';'QA'='Qatar';
    'RE'='Reunion';'RO'='Romania';'RS'='Serbia';'RU'='Russia';'RW'='Rwanda';
    'SA'='Saudi Arabia';'SB'='Solomon Islands';'SC'='Seychelles';'SD'='Sudan';'SE'='Sweden';
    'SG'='Singapore';'SH'='Saint Helena';'SI'='Slovenia';'SJ'='Svalbard and Jan Mayen';
    'SK'='Slovakia';'SL'='Sierra Leone';'SM'='San Marino';'SN'='Senegal';'SO'='Somalia';
    'SR'='Suriname';'SS'='South Sudan';'ST'='Sao Tome and Principe';'SV'='El Salvador';
    'SX'='Sint Maarten';'SY'='Syria';'SZ'='Eswatini';'TC'='Turks and Caicos Islands';
    'TD'='Chad';'TF'='French Southern Territories';'TG'='Togo';'TH'='Thailand';
    'TJ'='Tajikistan';'TK'='Tokelau';'TL'='Timor-Leste';'TM'='Turkmenistan';'TN'='Tunisia';
    'TO'='Tonga';'TR'='Turkey';'TT'='Trinidad and Tobago';'TV'='Tuvalu';'TW'='Taiwan';
    'TZ'='Tanzania';'UA'='Ukraine';'UG'='Uganda';'UM'='United States Minor Outlying Islands';
    'US'='United States';'UY'='Uruguay';'UZ'='Uzbekistan';'VA'='Vatican City';
    'VC'='Saint Vincent and the Grenadines';'VE'='Venezuela';'VG'='British Virgin Islands';
    'VI'='US Virgin Islands';'VN'='Vietnam';'VU'='Vanuatu';'WF'='Wallis and Futuna';
    'WS'='Samoa';'YE'='Yemen';'YT'='Mayotte';'ZA'='South Africa';'ZM'='Zambia';'ZW'='Zimbabwe'
}
function Get-CountryName {
    param([string]$Code)
    if (-not $Code) { return '' }
    $c = $Code.ToUpper().Trim()
    if ($script:CountryNames.ContainsKey($c)) { return $script:CountryNames[$c] }
    return $c
}

# =========================================================
# HTTP helpers
# =========================================================
function Invoke-RadioApi {
    param([string]$Url)
    if (-not $Url) { return $null }
    if ($Url -notmatch '^https?://') {
        Write-Log "Invoke-RadioApi: bad URL '$Url'" 'WARN'
        return $null
    }
    try {
        $wc = New-Object System.Net.WebClient
        $wc.Encoding = [System.Text.Encoding]::UTF8
        $wc.Headers.Add('User-Agent', 'PowerShellRadio/1.0')
        $json = $wc.DownloadString($Url)
        $wc.Dispose()
        return $json | ConvertFrom-Json
    } catch {
        Write-Log "API error for $Url : $($_.Exception.Message)" 'ERROR'
        return $null
    }
}

function Test-ImageBytes {
    param([byte[]]$Bytes)
    if (-not $Bytes -or $Bytes.Length -lt 8) { return $false }
    if ($Bytes[0] -eq 0x89 -and $Bytes[1] -eq 0x50 -and $Bytes[2] -eq 0x4E -and $Bytes[3] -eq 0x47) { return $true }
    if ($Bytes[0] -eq 0xFF -and $Bytes[1] -eq 0xD8 -and $Bytes[2] -eq 0xFF) { return $true }
    if ($Bytes[0] -eq 0x47 -and $Bytes[1] -eq 0x49 -and $Bytes[2] -eq 0x46 -and $Bytes[3] -eq 0x38) { return $true }
    if ($Bytes[0] -eq 0x42 -and $Bytes[1] -eq 0x4D) { return $true }
    return $false
}

function Get-CachedImage {
    param([string]$Url, [string]$CacheKey = $null)
    if (-not $Url) { return $null }
    if ($Url -notmatch '^https?://') { return $null }

    $key = if ($CacheKey) { $CacheKey } else { $Url }
    if ($script:imgCache.ContainsKey($key)) { return $script:imgCache[$key] }

    $md5  = [System.Security.Cryptography.MD5]::Create()
    $hash = [System.BitConverter]::ToString($md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($Url))).Replace('-','')
    $localFile = Join-Path $imgCacheDir ($hash + '.bin')

    try {
        $bytes = $null
        if (Test-Path $localFile) { $bytes = [System.IO.File]::ReadAllBytes($localFile) }
        if (-not (Test-ImageBytes -Bytes $bytes)) {
            $bytes = $script:httpClient.GetByteArrayAsync($Url).GetAwaiter().GetResult()
            if (-not (Test-ImageBytes -Bytes $bytes)) {
                $script:imgCache[$key] = $null
                return $null
            }
            [System.IO.File]::WriteAllBytes($localFile, $bytes)
        }
        $ms  = New-Object System.IO.MemoryStream(,$bytes)
        $img = [System.Drawing.Image]::FromStream($ms)
        $script:imgCache[$key] = $img
        return $img
    } catch {
        $script:imgCache[$key] = $null
        return $null
    }
}

function Get-FlagImage {
    param([string]$CountryCode)
    if (-not $CountryCode) { return $null }
    $cc = $CountryCode.ToLower().Trim()
    if ($cc.Length -ne 2) { return $null }
    if ($script:flagCache.ContainsKey($cc)) { return $script:flagCache[$cc] }
    $url = "https://flagcdn.com/w40/$cc.png"
    $img = Get-CachedImage -Url $url -CacheKey "flag_$cc"
    $script:flagCache[$cc] = $img
    return $img
}

# =========================================================
# Station model
# =========================================================
function ConvertTo-Station {
    param($item, [bool]$isFav = $false)
    [PSCustomObject]@{
        Id          = $item.stationuuid
        Name        = $item.name
        Url         = $item.url_resolved
        ImageUrl    = if ($item.favicon) { $item.favicon } else { '' }
        CountryCode = $item.countrycode
        CountryName = Get-CountryName $item.countrycode
        Languages   = ($item.language -split ',') -join ', '
        Tags        = ($item.tags -split ',') -join ', '
        Codec       = $item.codec
        Bitrate     = $item.bitrate
        IsFavorite  = $isFav
    }
}

function Load-Favorites {
    if (Test-Path $favPath) {
        try {
            $json = Get-Content $favPath -Raw -Encoding UTF8 | ConvertFrom-Json
            foreach ($s in $json) {
                [void]$script:favStations.Add([PSCustomObject]@{
                    Id = $s.Id; Name = $s.Name; Url = $s.Url; ImageUrl = $s.ImageUrl
                    CountryCode = $s.CountryCode; CountryName = $s.CountryName
                    Languages = $s.Languages; Tags = $s.Tags
                    Codec = $s.Codec; Bitrate = $s.Bitrate; IsFavorite = $true
                })
            }
        } catch { Write-Log "Load-Favorites: $($_.Exception.Message)" 'ERROR' }
    }
}

function Save-Favorites {
    try {
        $script:favStations | ConvertTo-Json -Depth 5 | Set-Content $favPath -Encoding UTF8
    } catch { Write-Log "Save-Favorites: $($_.Exception.Message)" 'ERROR' }
}

# =========================================================
# Codec support check
# WMP supports: MP3, AAC, WMA, WAV, WMV. Does NOT support: Opus, Vorbis, FLAC.
# =========================================================
function Test-CodecSupported {
    param([string]$Codec)
    if (-not $Codec) { return $true }
    $c = $Codec.ToUpper()
    if ($c -match 'OPUS')   { return $false }
    if ($c -match 'VORBIS') { return $false }
    if ($c -match 'FLAC')   { return $false }
    return $true
}

# =========================================================
# ICY metadata
# =========================================================
function Get-IcyMetadata {
    param(
        [string]$Url,
        [int]$TimeoutMs = 2500
    )
    if (-not $Url) { return $null }

    try {
        $req = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Get, $Url)
        $req.Headers.TryAddWithoutValidation('Icy-MetaData', '1') | Out-Null
        $req.Headers.TryAddWithoutValidation('User-Agent', 'PowerShellRadio/1.0') | Out-Null

        $client = [System.Net.Http.HttpClient]::new()
        $client.Timeout = [TimeSpan]::FromMilliseconds($TimeoutMs)

        $resp = $client.SendAsync($req, [System.Net.Http.HttpCompletionOption]::ResponseHeadersRead).GetAwaiter().GetResult()
        if (-not $resp.IsSuccessStatusCode) {
            $resp.Dispose(); $client.Dispose(); $req.Dispose()
            return $null
        }

        $metaInt = 0
        $v = $null
        if ($resp.Headers.TryGetValues('icy-metaint', [ref]$v)) {
            [void][int]::TryParse((@($v)[0]), [ref]$metaInt)
        } elseif ($resp.Content.Headers.TryGetValues('icy-metaint', [ref]$v)) {
            [void][int]::TryParse((@($v)[0]), [ref]$metaInt)
        }

        if ($metaInt -le 0) {
            $resp.Dispose(); $client.Dispose(); $req.Dispose()
            return $null
        }

        $stream = $resp.Content.ReadAsStreamAsync().GetAwaiter().GetResult()

        $buf = New-Object byte[] $metaInt
        $read = 0
        while ($read -lt $metaInt) {
            $n = $stream.Read($buf, $read, $metaInt - $read)
            if ($n -le 0) { break }
            $read += $n
        }
        if ($read -lt $metaInt) {
            $stream.Dispose(); $resp.Dispose(); $client.Dispose(); $req.Dispose()
            return $null
        }

        $lenByte = $stream.ReadByte()
        if ($lenByte -lt 0) {
            $stream.Dispose(); $resp.Dispose(); $client.Dispose(); $req.Dispose()
            return $null
        }
        $metaLen = $lenByte * 16
        if ($metaLen -le 0) {
            $stream.Dispose(); $resp.Dispose(); $client.Dispose(); $req.Dispose()
            return $null
        }

        $metaBuf = New-Object byte[] $metaLen
        $metaRead = 0
        while ($metaRead -lt $metaLen) {
            $n = $stream.Read($metaBuf, $metaRead, $metaLen - $metaRead)
            if ($n -le 0) { break }
            $metaRead += $n
        }
        $metaStr = [System.Text.Encoding]::UTF8.GetString($metaBuf, 0, $metaRead).TrimEnd([char]0)

        $stream.Dispose()
        $resp.Dispose()
        $client.Dispose()
        $req.Dispose()

        $m = [regex]::Match($metaStr, "StreamTitle='([^']*)'")
        if (-not $m.Success) { return $null }
        $raw = $m.Groups[1].Value.Trim()
        if (-not $raw) { return $null }

        $attrMatches = [regex]::Matches($raw, '(\w+)="([^"]*)"')
        if ($attrMatches.Count -eq 0) {
            if ($raw.EndsWith('-')) { $raw = $raw.Substring(0, $raw.Length - 1).Trim() }
            return $raw
        }

        $firstAttr = $attrMatches[0]
        $firstIdx  = $raw.IndexOf($firstAttr.Value)
        $mainTitle = if ($firstIdx -gt 0) {
            $raw.Substring(0, $firstIdx).Trim().TrimEnd('-').Trim()
        } else { '' }

        $attrs = @{}
        foreach ($am in $attrMatches) { $attrs[$am.Groups[1].Value] = $am.Groups[2].Value }

        $textAttr = if ($attrs.ContainsKey('text')) { $attrs['text'] } else { '' }
        $title = if ($textAttr) {
            if (-not $mainTitle) { $textAttr } else { "$mainTitle - $textAttr" }
        } else { $mainTitle }

        if (-not $title) { return $null }
        return $title
    } catch {
        return $null
    }
}

# =========================================================
# Form
# =========================================================
$form               = New-Object System.Windows.Forms.Form
$form.Text          = 'Radio Browser (PowerShell)'
$form.Width         = 840
$form.Height        = 720
$form.MinimumSize   = New-Object System.Drawing.Size(430, 400)
$form.StartPosition = 'CenterScreen'
$form.BackColor     = $script:ClrBg
$form.ForeColor     = $script:ClrText
$form.Font          = New-Object System.Drawing.Font('Segoe UI', 9)

$form.add_HandleCreated({
    try {
        $h = $form.Handle
        if ($h -eq [IntPtr]::Zero) { return }
        $ex = [Win32Native]::GetWindowLong($h, -20)
        $ex = $ex -bor 0x02000000
        [void][Win32Native]::SetWindowLong($h, -20, $ex)
    } catch { }
})

$tabs                = New-Object FlatTabControl
$tabs.Dock           = 'Fill'
$tabs.TabBackColor       = $script:ClrBg
$tabs.TabActiveBackColor = $script:ClrCardBg
$tabs.TabTextColor       = $script:ClrTextDim
$tabs.TabActiveTextColor = $script:ClrText
$tabs.AccentColor        = $script:ClrAccent
$tabs.ActiveFont         = $script:FontTabBold
$tabs.InactiveFont       = $script:FontTab

$searchTab           = New-Object System.Windows.Forms.TabPage
$searchTab.Text      = 'Search'
$searchTab.BackColor = $script:ClrBg
$searchTab.ForeColor = $script:ClrText

$favTab              = New-Object System.Windows.Forms.TabPage
$favTab.Text         = 'Favorites'
$favTab.BackColor    = $script:ClrBg
$favTab.ForeColor    = $script:ClrText

$tabs.TabPages.Add($searchTab) | Out-Null
$tabs.TabPages.Add($favTab)    | Out-Null

# --- Search panel ---
$searchPanel           = New-Object System.Windows.Forms.Panel
$searchPanel.Dock      = 'Top'
$searchPanel.Height    = 42
$searchPanel.BackColor = $script:ClrBg

$countryCombo              = New-Object System.Windows.Forms.ComboBox
$countryCombo.Left         = 4
$countryCombo.Top          = 8
$countryCombo.Width        = 220
$countryCombo.DropDownStyle = 'DropDownList'
$countryCombo.FlatStyle    = 'Flat'
$countryCombo.BackColor    = $script:ClrCardBg
$countryCombo.ForeColor    = $script:ClrText
$countryCombo.Font         = New-Object System.Drawing.Font('Segoe UI', 9)

$tagCombo                  = New-Object System.Windows.Forms.ComboBox
$tagCombo.Left             = 232
$tagCombo.Top              = 8
$tagCombo.Width            = 160
$tagCombo.DropDownStyle    = 'DropDown'
$tagCombo.FlatStyle        = 'Flat'
$tagCombo.BackColor        = $script:ClrCardBg
$tagCombo.ForeColor        = $script:ClrText
$tagCombo.Font             = New-Object System.Drawing.Font('Segoe UI', 9)

$nameBox                   = New-Object System.Windows.Forms.TextBox
$nameBox.Left              = 400
$nameBox.Top               = 8
$nameBox.Width             = 240
$nameBox.Height            = 24
$nameBox.BorderStyle       = 'FixedSingle'
$nameBox.BackColor         = $script:ClrCardBg
$nameBox.ForeColor         = $script:ClrText
$nameBox.Font              = New-Object System.Drawing.Font('Segoe UI', 9)

$searchBtn                 = New-Object RoundButton
$searchBtn.Left            = 650
$searchBtn.Top             = 6
$searchBtn.Width           = 104
$searchBtn.Height          = 28
$searchBtn.Text            = 'Search'
$searchBtn.ForeColor       = [System.Drawing.Color]::White
$searchBtn.Font            = New-Object System.Drawing.Font('Segoe UI', 9, [System.Drawing.FontStyle]::Bold)
$searchBtn.CornerRadius    = 8
$searchBtn.NormalBack      = $script:ClrAccent
$searchBtn.HoverBack       = [System.Drawing.Color]::FromArgb(0,140,232)
$searchBtn.PressBack       = [System.Drawing.Color]::FromArgb(0,100,180)

$searchPanel.Controls.AddRange(@($countryCombo, $tagCombo, $nameBox, $searchBtn))

# --- Card containers ---
$listHost              = New-Object System.Windows.Forms.Panel
$listHost.Dock         = 'Fill'
$listHost.BackColor    = $script:ClrBg
$listHost.AutoScroll   = $true

$favHost               = New-Object System.Windows.Forms.Panel
$favHost.Dock          = 'Fill'
$favHost.BackColor     = $script:ClrBg
$favHost.AutoScroll    = $true

$searchTab.Controls.Add($listHost)
$searchTab.Controls.Add($searchPanel)
$favTab.Controls.Add($favHost)

# --- Player panel ---
$playerPanel           = New-Object System.Windows.Forms.Panel
$playerPanel.Dock      = 'Bottom'
$playerPanel.Height    = 68
$playerPanel.BackColor = [System.Drawing.Color]::FromArgb(40,40,40)

$nowPlayingLabel           = New-Object System.Windows.Forms.Label
$nowPlayingLabel.Left      = 12
$nowPlayingLabel.Top       = 12
$nowPlayingLabel.Width     = 320
$nowPlayingLabel.Height    = 44
$nowPlayingLabel.Text      = 'No station selected'
$nowPlayingLabel.ForeColor = $script:ClrText
$nowPlayingLabel.AutoEllipsis = $true
$nowPlayingLabel.Font      = $script:FontTitle

$youtubeBtn            = New-Object RoundButton
$youtubeBtn.Left       = 340
$youtubeBtn.Top        = 18
$youtubeBtn.Width      = 96
$youtubeBtn.Height     = 32
$youtubeBtn.Text       = 'YouTube'
$youtubeBtn.ForeColor  = $script:ClrText
$youtubeBtn.CornerRadius = 8
$youtubeBtn.NormalBack = $script:ClrCardBg
$youtubeBtn.HoverBack  = $script:ClrCardSel
$youtubeBtn.PressBack  = $script:ClrBorder
$youtubeBtn.BorderColor = $script:ClrBorder

$favBtn            = New-Object RoundButton
$favBtn.Left       = 442
$favBtn.Top        = 18
$favBtn.Width      = 106
$favBtn.Height     = 32
$favBtn.Text       = '[+] Fav'
$favBtn.ForeColor  = $script:ClrText
$favBtn.CornerRadius = 8
$favBtn.NormalBack = $script:ClrCardBg
$favBtn.HoverBack  = $script:ClrCardSel
$favBtn.PressBack  = $script:ClrBorder
$favBtn.BorderColor = $script:ClrBorder

$volumeLabel           = New-Object System.Windows.Forms.Label
$volumeLabel.Left      = 558
$volumeLabel.Top       = 26
$volumeLabel.Width     = 40
$volumeLabel.Text      = 'Vol:'
$volumeLabel.ForeColor = $script:ClrText

$volumeSlider              = New-Object System.Windows.Forms.TrackBar
$volumeSlider.Left         = 596
$volumeSlider.Top          = 14
$volumeSlider.Width        = 100
$volumeSlider.Minimum      = 0
$volumeSlider.Maximum      = 100
$volumeSlider.Value        = 50
$volumeSlider.TickFrequency = 10

$playBtn            = New-Object RoundButton
$playBtn.Left       = 702
$playBtn.Top        = 18
$playBtn.Width      = 90
$playBtn.Height     = 32
$playBtn.Text       = '> Play'
$playBtn.ForeColor  = [System.Drawing.Color]::White
$playBtn.CornerRadius = 8
$playBtn.NormalBack = $script:ClrAccent
$playBtn.HoverBack  = [System.Drawing.Color]::FromArgb(0,140,232)
$playBtn.PressBack  = [System.Drawing.Color]::FromArgb(0,100,180)

$playerPanel.Controls.AddRange(@($nowPlayingLabel, $youtubeBtn, $favBtn, $volumeLabel, $volumeSlider, $playBtn))

$form.Controls.Add($tabs)
$form.Controls.Add($playerPanel)

# =========================================================
# Card builder
# =========================================================
function New-StationCardControl {
    param($station)

    $card = New-Object System.Windows.Forms.Panel
    $card.Width  = $script:CardWidth
    $card.Height = $script:CardHeight
    $card.BackColor = $script:ClrCardBg
    $card.Tag = $station
    $card.Cursor = 'Hand'

    try {
        $prop = $card.GetType().GetProperty('DoubleBuffered', [System.Reflection.BindingFlags]'Instance,NonPublic')
        if ($prop) { $prop.SetValue($card, $true, $null) }
    } catch { }

    $card | Add-Member -NotePropertyName IsSelected -NotePropertyValue $false -Force
    $card | Add-Member -NotePropertyName IsHover    -NotePropertyValue $false -Force
    $card | Add-Member -NotePropertyName IconImage  -NotePropertyValue $null   -Force

    if ($station.ImageUrl) {
        if ($script:imgCache.ContainsKey($station.ImageUrl)) {
            $card.IconImage = $script:imgCache[$station.ImageUrl]
        } else {
            $script:iconQueue.Enqueue([PSCustomObject]@{ Station = $station; Card = $card })
        }
    }

    $card.add_Paint({
        param($sender, $e)
        $g = $e.Graphics
        $g.SmoothingMode = 'AntiAlias'
        $g.TextRenderingHint = 'ClearTypeGridFit'

        $st = $sender.Tag
        $sel = [bool]$sender.IsSelected
        $hov = [bool]$sender.IsHover

        $rect = New-Object System.Drawing.Rectangle(0, 0, ($sender.Width - 1), ($sender.Height - 1))
        $cardColor = if ($sel) { $script:ClrCardSel } elseif ($hov) { $script:ClrCardHover } else { $script:ClrCardBg }

        $d = 16
        $path = New-Object System.Drawing.Drawing2D.GraphicsPath
        $path.AddArc(0, 0, $d, $d, 180, 90)
        $path.AddArc(($rect.Right - $d), 0, $d, $d, 270, 90)
        $path.AddArc(($rect.Right - $d), ($rect.Bottom - $d), $d, $d, 0, 90)
        $path.AddArc(0, ($rect.Bottom - $d), $d, $d, 90, 90)
        $path.CloseFigure()
        $g.FillPath((New-Object System.Drawing.SolidBrush($cardColor)), $path)
        if (-not $sel) {
            $borderCol = if ($hov) { $script:ClrAccent } else { $script:ClrBorder }
            $g.DrawPath((New-Object System.Drawing.Pen($borderCol, 1)), $path)
        }

        $iconSize = 56
        $iconX = 8
        $iconY = [int](($sender.Height - $iconSize) / 2)
        if ($sender.IconImage) {
            try { $g.DrawImage($sender.IconImage, $iconX, $iconY, $iconSize, $iconSize) } catch { }
        } else {
            $g.FillRectangle((New-Object System.Drawing.SolidBrush($script:ClrBorder)), $iconX, $iconY, $iconSize, $iconSize)
            $sf = New-Object System.Drawing.StringFormat
            $sf.Alignment = 'Center'; $sf.LineAlignment = 'Center'
            $g.DrawString('RAD', $script:FontMeta, (New-Object System.Drawing.SolidBrush($script:ClrTextDim)),
                (New-Object System.Drawing.RectangleF($iconX, $iconY, $iconSize, $iconSize)), $sf)
        }

        $textLeft = $iconX + $iconSize + 10
        $starSize = 18
        $textRight = $sender.Width - $starSize - 14
        $textW = $textRight - $textLeft
        if ($textW -lt 40) { $textW = 40 }

        $line1Y = 8
        $flagX = $textLeft
        $flagY = $line1Y + 3
        $flagImg = Get-FlagImage -CountryCode $st.CountryCode
        if ($flagImg) {
            try { $g.DrawImage($flagImg, $flagX, $flagY, 22, 15) } catch { }
        }
        $countryText = $st.CountryName
        if (-not $countryText) { $countryText = $st.CountryCode }
        $countryW = 100
        [System.Windows.Forms.TextRenderer]::DrawText(
            $g, [string]$countryText, $script:FontMeta,
            (New-Object System.Drawing.Rectangle(($flagX + 26), $line1Y, $countryW, 20)),
            $script:ClrTextDim,
            ([System.Windows.Forms.TextFormatFlags]::Left -bor
             [System.Windows.Forms.TextFormatFlags]::SingleLine -bor
             [System.Windows.Forms.TextFormatFlags]::EndEllipsis -bor
             [System.Windows.Forms.TextFormatFlags]::NoPrefix -bor
             [System.Windows.Forms.TextFormatFlags]::VerticalCenter))

        $nameLeft = $flagX + 26 + $countryW + 6
        $nameW = $textRight - $nameLeft
        if ($nameW -lt 40) { $nameW = 40 }
        [System.Windows.Forms.TextRenderer]::DrawText(
            $g, [string]$st.Name, $script:FontTitle,
            (New-Object System.Drawing.Rectangle($nameLeft, $line1Y, $nameW, 20)),
            $script:ClrText,
            ([System.Windows.Forms.TextFormatFlags]::Left -bor
             [System.Windows.Forms.TextFormatFlags]::SingleLine -bor
             [System.Windows.Forms.TextFormatFlags]::EndEllipsis -bor
             [System.Windows.Forms.TextFormatFlags]::NoPrefix -bor
             [System.Windows.Forms.TextFormatFlags]::VerticalCenter))

        $meta = "{0} : {1} kbps   {2}" -f $st.Codec, $st.Bitrate, $st.Languages
        [System.Windows.Forms.TextRenderer]::DrawText(
            $g, [string]$meta, $script:FontMeta,
            (New-Object System.Drawing.Rectangle($textLeft, 30, $textW, 18)),
            $script:ClrTextDim,
            ([System.Windows.Forms.TextFormatFlags]::Left -bor
             [System.Windows.Forms.TextFormatFlags]::SingleLine -bor
             [System.Windows.Forms.TextFormatFlags]::EndEllipsis -bor
             [System.Windows.Forms.TextFormatFlags]::NoPrefix -bor
             [System.Windows.Forms.TextFormatFlags]::VerticalCenter))

        if ($st.Tags) {
            [System.Windows.Forms.TextRenderer]::DrawText(
                $g, [string]$st.Tags, $script:FontTags,
                (New-Object System.Drawing.Rectangle($textLeft, 48, $textW, 16)),
                $script:ClrTextDim,
                ([System.Windows.Forms.TextFormatFlags]::Left -bor
                 [System.Windows.Forms.TextFormatFlags]::SingleLine -bor
                 [System.Windows.Forms.TextFormatFlags]::EndEllipsis -bor
                 [System.Windows.Forms.TextFormatFlags]::NoPrefix -bor
                 [System.Windows.Forms.TextFormatFlags]::VerticalCenter))
        }

        $starX = $sender.Width - $starSize - 8
        $starY = 8
        $cx = [double]($starX + [int]($starSize / 2))
        $cy = [double]($starY + [int]($starSize / 2))
        $rOuter = [double]$starSize / 2.0
        $rInner = $rOuter * 0.45
        $pts = New-Object 'System.Drawing.PointF[]' 10
        for ($i = 0; $i -lt 10; $i++) {
            $r = if ($i % 2 -eq 0) { $rOuter } else { $rInner }
            $a = [Math]::PI / 5 * $i - [Math]::PI / 2
            $pts[$i] = New-Object System.Drawing.PointF(
                ([double]($cx + $r * [Math]::Cos($a))),
                ([double]($cy + $r * [Math]::Sin($a))))
        }
        if ($st.IsFavorite) {
            $g.FillPolygon((New-Object System.Drawing.SolidBrush($script:ClrStar)), $pts)
        } else {
            $g.DrawPolygon((New-Object System.Drawing.Pen($script:ClrStar, 1.2)), $pts)
        }
    })

    $card.add_MouseEnter({
        param($sender, $e)
        $sender.IsHover = $true
        $sender.Invalidate()
    })
    $card.add_MouseLeave({
        param($sender, $e)
        $sender.IsHover = $false
        $sender.Invalidate()
    })
    $card.add_Click({
        param($sender, $e)
        if ($script:selectedCard -and $script:selectedCard -ne $sender) {
            $script:selectedCard.IsSelected = $false
            $script:selectedCard.Invalidate()
        }
        $script:selectedCard = $sender
        $sender.IsSelected = $true
        $sender.Invalidate()
        Set-CurrentStation $sender.Tag
    })
    $card.add_DoubleClick({
        param($sender, $e)
        if ($wmp.playState -eq $script:WMPS_PLAYING) { Stop-Playback }
        Start-Playback
    })

    return $card
}

# =========================================================
# Grid layout
# =========================================================
function Get-GridLayout {
    param(
        [System.Windows.Forms.Panel]$hostPanel,
        [int]$cardCount
    )
    if ($cardCount -eq 0) {
        return [PSCustomObject]@{ Cols = 1; Rows = 0; TotalHeight = 0 }
    }
    $gapX = $script:CardGapX
    $gapY = $script:CardGapY
    $cw   = $script:CardWidth
    $ch   = $script:CardHeight

    $clientW = $hostPanel.ClientSize.Width - 4
    $cols = [int][Math]::Floor(($clientW + $gapX) / ($cw + $gapX))
    if ($cols -lt 1) { $cols = 1 }

    $rows = [int][Math]::Ceiling($cardCount / $cols)
    $totalH = $rows * ($ch + $gapY) + $gapY
    return [PSCustomObject]@{ Cols = $cols; Rows = $rows; TotalHeight = $totalH }
}

function Set-CardPosition {
    param(
        [System.Windows.Forms.Panel]$hostPanel,
        [System.Windows.Forms.Control]$card,
        [int]$index
    )
    $gapX = $script:CardGapX
    $gapY = $script:CardGapY
    $cw   = $script:CardWidth
    $ch   = $script:CardHeight

    $clientW = $hostPanel.ClientSize.Width - 4
    $cols = [int][Math]::Floor(($clientW + $gapX) / ($cw + $gapX))
    if ($cols -lt 1) { $cols = 1 }

    $row = [int][Math]::Floor($index / $cols)
    $col = $index % $cols
    $x = $col * ($cw + $gapX)
    $y = $row * ($ch + $gapY)
    $card.Location = New-Object System.Drawing.Point($x, $y)
}

function Update-ScrollSize {
    param(
        [System.Windows.Forms.Panel]$hostPanel,
        [System.Collections.ArrayList]$cards
    )
    $maxBottom = 0
    foreach ($c in $cards) {
        if ($c.IsDisposed) { continue }
        if ($c.Bottom -gt $maxBottom) { $maxBottom = $c.Bottom }
    }
    $h = $maxBottom + $script:CardGapY
    if ($h -lt 100) { $h = 100 }
    $hostPanel.AutoScrollMinSize = New-Object System.Drawing.Size(0, $h)
}

function Layout-Cards {
    param(
        [System.Windows.Forms.Panel]$hostPanel,
        [System.Collections.ArrayList]$cards
    )
    if ($cards.Count -eq 0) { return }

    $layout = Get-GridLayout -hostPanel $hostPanel -cardCount $cards.Count

    $hostPanel.SuspendLayout()
    $i = 0
    foreach ($card in $cards) {
        if ($card.IsDisposed) { continue }
        $row = [int][Math]::Floor($i / $layout.Cols)
        $col = $i % $layout.Cols
        $x = $col * ($script:CardWidth + $script:CardGapX)
        $y = $row * ($script:CardHeight + $script:CardGapY)
        $card.Location = New-Object System.Drawing.Point($x, $y)
        $i++
    }
    $hostPanel.ResumeLayout()
    Update-ScrollSize -hostPanel $hostPanel -cards $cards
}

function Clear-Host-Panel {
    param(
        [System.Windows.Forms.Panel]$hostPanel,
        [System.Collections.ArrayList]$cards
    )
    $hostPanel.SuspendLayout()
    foreach ($c in @($cards)) {
        try { $hostPanel.Controls.Remove($c); $c.Dispose() } catch { }
    }
    $cards.Clear()
    $hostPanel.ResumeLayout()
}

# =========================================================
# Async loading (Runspace based)
# =========================================================
function Start-AsyncLoad-MoreStations {
    if ($script:searchInFlight) { return }
    if (-not $script:searchHasMore) { return }
    if (-not $script:searchQuery) {
        Write-Log "Start-AsyncLoad: no query set yet, skip" 'WARN'
        return
    }
    $script:searchInFlight = $true

    $sep = if ($script:searchQuery -match '\?') { '&' } else { '?' }
    $url = "$($script:searchQuery)$($sep)limit=$($script:searchLimit)&offset=$($script:searchOffset)"

    Write-Log "AsyncLoad start: offset=$($script:searchOffset)" 'DEBUG'

    try {
        $rs = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace()
        $rs.ApartmentState = 'MTA'
        $rs.ThreadOptions  = 'ReuseThread'
        $rs.Open()

        $ps = [System.Management.Automation.PowerShell]::Create()
        $ps.Runspace = $rs
        [void]$ps.AddScript({
            param($Url)
            try {
                $wc = New-Object System.Net.WebClient
                $wc.Encoding = [System.Text.Encoding]::UTF8
                $wc.Headers.Add('User-Agent', 'PowerShellRadio/1.0')
                $json = $wc.DownloadString($Url)
                $wc.Dispose()
                return $json
            } catch {
                return $null
            }
        }).AddArgument($url)

        $asyncResult = $ps.BeginInvoke()

        $script:asyncPending = [PSCustomObject]@{
            PS          = $ps
            RS          = $rs
            AsyncResult = $asyncResult
        }
    } catch {
        Write-Log "Start-AsyncLoad error: $($_.Exception.Message)" 'ERROR'
        $script:searchInFlight = $false
    }
}

function Poll-AsyncLoad {
    if (-not $script:asyncPending) { return }
    $pending = $script:asyncPending
    if (-not $pending.AsyncResult.IsCompleted) { return }

    $script:asyncPending = $null
    $json = $null
    try {
        $out = $pending.PS.EndInvoke($pending.AsyncResult)
        if ($out -and $out.Count -gt 0) { $json = [string]$out[0] }
    } catch {
        Write-Log "Poll-AsyncLoad EndInvoke error: $($_.Exception.Message)" 'WARN'
    } finally {
        try { $pending.PS.Dispose() } catch { }
        try { $pending.RS.Close(); $pending.RS.Dispose() } catch { }
    }

    if (-not $json) {
        Write-Log "AsyncLoad: empty response, will retry on next scroll" 'WARN'
        $script:searchInFlight = $false
        return
    }

    $resp = $null
    try { $resp = $json | ConvertFrom-Json } catch { }
    if (-not $resp) {
        Write-Log "AsyncLoad: bad json" 'WARN'
        $script:searchInFlight = $false
        return
    }

    $stations = @($resp)
    if ($stations.Count -eq 0) {
        $script:searchHasMore = $false
        $script:searchInFlight = $false
        Write-Log "AsyncLoad: no more stations" 'DEBUG'
        return
    }

    $wasAtTop = ($listHost.AutoScrollPosition.Y -eq 0)

    $favIds = $script:favStations | ForEach-Object { $_.Id }
    $listHost.SuspendLayout()
    foreach ($s in $stations) {
        $st = ConvertTo-Station $s ($favIds -contains $s.stationuuid)
        [void]$script:allStations.Add($st)
        $card = New-StationCardControl -station $st
        $idx = $script:searchCards.Count
        [void]$script:searchCards.Add($card)
        $listHost.Controls.Add($card)
        Set-CardPosition -hostPanel $listHost -card $card -index $idx
    }
    $listHost.ResumeLayout()

    $script:searchOffset += $stations.Count
    if ($stations.Count -lt $script:searchLimit) {
        $script:searchHasMore = $false
        Write-Log "AsyncLoad: reached end" 'DEBUG'
    }

    Update-ScrollSize -hostPanel $listHost -cards $script:searchCards

    if ($wasAtTop) {
        $listHost.AutoScrollPosition = New-Object System.Drawing.Point(0, 0)
    }

    $script:searchInFlight = $false
    Write-Log "AsyncLoad done: offset=$($script:searchOffset), total=$($script:searchCards.Count)" 'DEBUG'
}

function Fill-SearchViewport {
    if ($script:searchHasMore `
        -and -not $script:searchInFlight `
        -and $script:searchQuery `
        -and ($script:searchCards.Count * ($script:CardHeight + $script:CardGapY)) -lt ($listHost.ClientSize.Height + 100)) {
        Start-AsyncLoad-MoreStations
    }
}

function Refresh-FavListBox {
    Clear-Host-Panel -hostPanel $favHost -cards $script:favCards
    $favHost.SuspendLayout()
    foreach ($s in $script:favStations) {
        $card = New-StationCardControl -station $s
        $idx = $script:favCards.Count
        [void]$script:favCards.Add($card)
        $favHost.Controls.Add($card)
        Set-CardPosition -hostPanel $favHost -card $card -index $idx
    }
    $favHost.ResumeLayout()
    Update-ScrollSize -hostPanel $favHost -cards $script:favCards
}

# --- Scroll trigger ---
$listHost.add_Scroll({
    if (-not $script:searchHasMore) { return }
    if ($script:searchInFlight) { return }
    if (-not $script:searchQuery) { return }
    $scrollY  = -$listHost.AutoScrollPosition.Y
    $clientH  = $listHost.ClientSize.Height
    $contentH = $listHost.AutoScrollMinSize.Height
    if ($scrollY + $clientH + 300 -ge $contentH) {
        Start-AsyncLoad-MoreStations
    }
})

# --- Resize triggers ---
$listHost.add_Resize({
    Layout-Cards -hostPanel $listHost -cards $script:searchCards
    if ($script:searchHasMore -and -not $script:searchInFlight -and $script:searchQuery) {
        Fill-SearchViewport
    }
})
$favHost.add_Resize({
    Layout-Cards -hostPanel $favHost -cards $script:favCards
})

# =========================================================
# Mouse wheel
# =========================================================
function Scroll-ActivePanel {
    param([int]$notches)
    $target = $null
    if ($tabs.SelectedIndex -eq 0) { $target = $listHost }
    elseif ($tabs.SelectedIndex -eq 1) { $target = $favHost }
    if (-not $target) { return }

    $step = 60
    $cur = -$target.AutoScrollPosition.Y
    $max = $target.AutoScrollMinSize.Height - $target.ClientSize.Height
    if ($max -lt 0) { $max = 0 }
    $new = $cur - ($notches * $step)
    if ($new -lt 0)   { $new = 0 }
    if ($new -gt $max){ $new = $max }
    $target.AutoScrollPosition = New-Object System.Drawing.Point(0, $new)
}

$form.add_MouseWheel({
    param($sender, $e)
    $notches = [int]($e.Delta / 120)
    Scroll-ActivePanel -notches $notches
})

$listHost.add_MouseWheel({
    param($sender, $e)
    $notches = [int]($e.Delta / 120)
    Scroll-ActivePanel -notches $notches
})
$favHost.add_MouseWheel({
    param($sender, $e)
    $notches = [int]($e.Delta / 120)
    Scroll-ActivePanel -notches $notches
})

# =========================================================
# Auto-load timer
# =========================================================
$autoLoadTimer = New-Object System.Windows.Forms.Timer
$autoLoadTimer.Interval = 200
$autoLoadTimer.add_Tick({
    try {
        Poll-AsyncLoad
        if (-not $script:searchHasMore) { return }
        if ($script:searchInFlight) { return }
        if (-not $script:searchQuery) { return }
        if ($tabs.SelectedIndex -ne 0) { return }
        $scrollY  = -$listHost.AutoScrollPosition.Y
        $clientH  = $listHost.ClientSize.Height
        $contentH = $listHost.AutoScrollMinSize.Height
        if ($scrollY + $clientH + 300 -ge $contentH) {
            Start-AsyncLoad-MoreStations
        }
    } catch {
        Write-Log "autoLoadTimer: $($_.Exception.Message)" 'WARN'
    }
})
$autoLoadTimer.Start()

# =========================================================
# Icon loader timer
# =========================================================
$iconTimer = New-Object System.Windows.Forms.Timer
$iconTimer.Interval = 60
$iconTimer.add_Tick({
    try {
        if ($script:iconQueue.Count -eq 0) { return }
        $entry = $script:iconQueue.Dequeue()
        if (-not $entry) { return }
        if ($entry.Card.IsDisposed) { return }
        $img = Get-CachedImage -Url $entry.Station.ImageUrl
        if ($img -and -not $entry.Card.IsDisposed) {
            $entry.Card.IconImage = $img
            $entry.Card.Invalidate()
        }
    } catch {
        Write-Log "iconTimer: $($_.Exception.Message)" 'WARN'
    }
})
$iconTimer.Start()

# =========================================================
# Data loading
# =========================================================
function Load-Countries {
    $countryCombo.Items.Clear()
    [void]$countryCombo.Items.Add('(All countries)')

    $src = $null
    if (Test-Path $countriesPath) {
        try { $src = Get-Content $countriesPath -Raw -Encoding UTF8 | ConvertFrom-Json } catch { }
    }
    if (-not $src) {
        $src = Invoke-RadioApi 'https://de1.api.radio-browser.info/json/countries'
        if ($src) {
            $src = $src | Select-Object iso_3166_1, stationcount
            $src | ConvertTo-Json | Set-Content $countriesPath -Encoding UTF8
        }
    }
    if (-not $src) { return }

    $items = @()
    foreach ($c in $src) {
        $code  = if ($c.iso_3166_1) { $c.iso_3166_1 } else { $c.Name }
        $count = if ($c.stationcount -ne $null) { [int]$c.stationcount } else { [int]$c.Count }
        $items += [PSCustomObject]@{ Code = $code; Count = $count }
    }
    $sorted = $items | Sort-Object Count -Descending
    foreach ($it in $sorted) {
        $name = Get-CountryName $it.Code
        [void]$countryCombo.Items.Add([PSCustomObject]@{
            Name  = $it.Code
            Label = "$name ($($it.Count))"
            Count = $it.Count
        })
    }
    $countryCombo.DisplayMember = 'Label'
}

function Load-Tags {
    $tagCombo.Items.Clear()
    [void]$tagCombo.Items.Add('')

    $resp = Invoke-RadioApi 'https://de1.api.radio-browser.info/json/tags?order=stationcount&reverse=true&limit=300'
    if ($resp) {
        $names = @()
        foreach ($t in $resp) {
            $names += $t.name
            [void]$tagCombo.Items.Add($t.name)
        }
        $names | ConvertTo-Json | Set-Content $tagsPath -Encoding UTF8
        Write-Log "Loaded $($names.Count) tags sorted by stationcount desc"
    }
}

function Get-DefaultStations {
    $script:allStations.Clear()
    $script:selectedCard = $null
    Clear-Host-Panel -hostPanel $listHost -cards $script:searchCards
    $script:searchOffset   = 0
    $script:searchHasMore  = $true
    $script:searchInFlight = $false
    $script:searchQuery    = 'https://de1.api.radio-browser.info/json/stations/search?order=votes&reverse=true&hidebroken=true'
    Start-AsyncLoad-MoreStations
}

function Do-Search {
    $script:allStations.Clear()
    $script:selectedCard = $null
    Clear-Host-Panel -hostPanel $listHost -cards $script:searchCards
    $script:searchOffset   = 0
    $script:searchHasMore  = $true
    $script:searchInFlight = $false

    $name = $nameBox.Text.Trim()
    $tag = if ($tagCombo.Text) { $tagCombo.Text.Trim() } else { '' }
    $tagWasSelected = ($tagCombo.SelectedIndex -ge 0)

    $country = ''
    if ($countryCombo.SelectedItem -and $countryCombo.SelectedItem -is [PSCustomObject]) {
        $country = $countryCombo.SelectedItem.Name
    }

    $params = @()
    if ($name) { $params += "name=" + [uri]::EscapeDataString($name) }
    if ($tag) {
        $params += "tag=" + [uri]::EscapeDataString($tag)
        if ($tagWasSelected) {
            $params += 'tagExact=true'
        }
    }
    if ($country) { $params += "countrycode=" + [uri]::EscapeDataString($country) }
    $params += 'hidebroken=true'
    $params += 'order=votes'
    $params += 'reverse=true'

    $script:searchQuery = "https://de1.api.radio-browser.info/json/stations/search?" + ($params -join '&')

    Write-Log "Do-Search: query=$($script:searchQuery)" 'DEBUG'

    Start-AsyncLoad-MoreStations
}

# =========================================================
# Playback
# =========================================================
function Set-CurrentStation {
    param($station)
    $script:currentStation = $station
    if ($station) {
        $nowPlayingLabel.Text = $station.Name
        $playBtn.Enabled      = $true
        $favBtn.Enabled       = $true
        $favIds = $script:favStations | ForEach-Object { $_.Id }
        if ($favIds -contains $station.Id) { $favBtn.Text = '[-] Unfav' }
        else { $favBtn.Text = '[+] Fav' }
        $youtubeBtn.Enabled = [bool]$station.Name
    } else {
        $nowPlayingLabel.Text = 'No station selected'
        $playBtn.Enabled      = $false
        $favBtn.Enabled       = $false
        $youtubeBtn.Enabled   = $false
    }
    $playBtn.Invalidate(); $favBtn.Invalidate(); $youtubeBtn.Invalidate()
}

function Start-Playback {
    if (-not $script:currentStation) { return }

    # WMP cannot play Opus / Vorbis / FLAC. Warn and skip to avoid freeze.
    if (-not (Test-CodecSupported $script:currentStation.Codec)) {
        Write-Log "Unsupported codec: $($script:currentStation.Codec) — skipping playback" 'WARN'
        [System.Windows.Forms.MessageBox]::Show(
            "WMP does not support codec '$($script:currentStation.Codec)'.`n`nStation: $($script:currentStation.Name)",
            'Unsupported codec',
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Warning) | Out-Null
        return
    }

    try {
        $script:loggedMeta = $false
        $script:nowPlaying = $null
        $wmp.URL = $script:currentStation.Url
        $wmp.controls.play()
        $playBtn.Text = 'Stop'
        $playBtn.Invalidate()
        Write-Log "Play: $($script:currentStation.Name) -> $($script:currentStation.Url)"

        # Schedule initial ICY fetch in ~3 seconds
        $script:icyKickRestart = $true
    } catch {
        Write-Log "Playback error: $($_.Exception.Message)" 'ERROR'
    }
}

function Stop-Playback {
    try { $wmp.controls.stop() } catch { }
    $playBtn.Text = '> Play'
    $playBtn.Invalidate()
    $script:nowPlaying = $null
    $youtubeBtn.Enabled = $false
    $youtubeBtn.Invalidate()
}

function Toggle-Favorite {
    if (-not $script:currentStation) { return }
    $favIds = $script:favStations | ForEach-Object { $_.Id }
    if ($favIds -contains $script:currentStation.Id) {
        $toRemove = $script:favStations | Where-Object { $_.Id -eq $script:currentStation.Id }
        foreach ($x in $toRemove) { [void]$script:favStations.Remove($x) }
        $script:currentStation.IsFavorite = $false
        $favBtn.Text = '[+] Fav'
    } else {
        $script:currentStation.IsFavorite = $true
        [void]$script:favStations.Add($script:currentStation)
        $favBtn.Text = '[-] Unfav'
    }
    $favBtn.Invalidate()
    Save-Favorites
    Refresh-FavListBox
}

function Search-YouTube {
    $query = if ($script:nowPlaying) { $script:nowPlaying } else { $script:currentStation.Name }
    if (-not $query) { return }
    Start-Process ('https://www.youtube.com/results?search_query=' + [uri]::EscapeDataString($query))
}

function Update-NowPlaying {
    param([string]$Title)
    if (-not $Title) { return }
    if ($Title -eq $script:nowPlaying) { return }
    $script:nowPlaying = $Title
    $stationName = if ($script:currentStation) { $script:currentStation.Name } else { '' }
    $nowPlayingLabel.Text = if ($stationName) { "$stationName  -  $Title" } else { $Title }
    $youtubeBtn.Enabled = $true
    $youtubeBtn.Invalidate()
}

# =========================================================
# Events
# =========================================================
$searchBtn.add_Click({ Do-Search })
$nameBox.add_KeyDown({ if ($_.KeyCode -eq 'Enter') { Do-Search } })
$tagCombo.add_KeyDown({ if ($_.KeyCode -eq 'Enter') { Do-Search } })
$playBtn.add_Click({ if ($wmp.playState -eq $script:WMPS_PLAYING) { Stop-Playback } else { Start-Playback } })
$favBtn.add_Click({ Toggle-Favorite })
$youtubeBtn.add_Click({ Search-YouTube })
$volumeSlider.add_Scroll({ $wmp.settings.volume = $volumeSlider.Value })

# =========================================================
# NowPlaying timer (play/stop status only)
# =========================================================
$timer = New-Object System.Windows.Forms.Timer
$timer.Interval = 1000
$timer.add_Tick({
    try {
        if ($wmp.playState -eq $script:WMPS_PLAYING) {
            if ($playBtn.Text -ne 'Stop') { $playBtn.Text = 'Stop'; $playBtn.Invalidate() }
        } else {
            if ($playBtn.Text -ne '> Play') { $playBtn.Text = '> Play'; $playBtn.Invalidate() }
        }
    } catch {
        Write-Log "timer: $($_.Exception.Message)" 'WARN'
    }
})
$timer.Start()

# =========================================================
# ICY metadata timer (initial + periodic)
# =========================================================
$metaTimer = New-Object System.Windows.Forms.Timer
$metaTimer.Interval = 1000
$metaTimer.add_Tick({
    try {
        if (-not $script:currentStation) { return }
        if ($wmp.playState -ne $script:WMPS_PLAYING) { return }
        if ($script:icyInFlight) { return }

        $script:icyTickCounter = ($script:icyTickCounter + 1)
        $shouldFetch = $false
        if ($script:icyInitialDue) {
            $shouldFetch = $true
            $script:icyInitialDue = $false
            $script:icyTickCounter = 0
        } elseif ($script:icyTickCounter -ge 15) {
            $shouldFetch = $true
            $script:icyTickCounter = 0
        }
        if (-not $shouldFetch) { return }

        $script:icyInFlight = $true
        $url = $script:currentStation.Url
        $codec = $script:currentStation.Codec
        # Don't try ICY on codecs we can't play anyway
        if (-not (Test-CodecSupported $codec)) {
            $script:icyInFlight = $false
            return
        }
        try {
            $icyTitle = Get-IcyMetadata -Url $url -TimeoutMs 2500
            if ($icyTitle) {
                Update-NowPlaying -Title $icyTitle
                Write-Log "ICY: $icyTitle" 'DEBUG'
            }
        } catch {
            Write-Log "icy fetch: $($_.Exception.Message)" 'WARN'
        } finally {
            $script:icyInFlight = $false
        }
    } catch {
        Write-Log "metaTimer: $($_.Exception.Message)" 'WARN'
        $script:icyInFlight = $false
    }
})
$metaTimer.Start()

# =========================================================
# ICY kick timer (fires initial fetch ~3s after start)
# =========================================================
$icyKickTimer = New-Object System.Windows.Forms.Timer
$icyKickTimer.Interval = 200
$icyKickTimer.add_Tick({
    try {
        if ($script:icyKickRestart) {
            $script:icyKickRestart = $false
            $script:icyInitialDelay = 15
            $script:icyInitialDue = $false
        }
        if ($script:icyInitialDelay -gt 0) {
            $script:icyInitialDelay--
            if ($script:icyInitialDelay -eq 0) {
                $script:icyInitialDue = $true
            }
        }
    } catch { }
})
$icyKickTimer.Start()

# =========================================================
# FormClosing (declare AFTER all timers)
# =========================================================
$form.add_FormClosing({
    Save-Favorites
    try { $wmp.controls.stop() } catch { }
    try { [System.Runtime.InteropServices.Marshal]::ReleaseComObject($wmp) | Out-Null } catch { }
    try { $script:httpClient.Dispose() } catch { }
    try { $iconTimer.Stop() } catch { }
    try { $timer.Stop() } catch { }
    try { $metaTimer.Stop() } catch { }
    try { $icyKickTimer.Stop() } catch { }
    try { $autoLoadTimer.Stop() } catch { }
    if ($script:asyncPending) {
        try { $script:asyncPending.PS.Stop() } catch { }
        try { $script:asyncPending.PS.Dispose() } catch { }
        try { $script:asyncPending.RS.Close(); $script:asyncPending.RS.Dispose() } catch { }
    }
})

# =========================================================
# Startup
# =========================================================
Load-Favorites
Refresh-FavListBox
[void][System.Windows.Forms.Application]::DoEvents()

$form.Add_Shown({
    Load-Countries
    Load-Tags
    Get-DefaultStations
})

[void]$form.ShowDialog()