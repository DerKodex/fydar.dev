$ErrorActionPreference = "Stop"

function Get-Emoji([int]$hex) { return [char]::ConvertFromUtf32($hex) }

# --- Metadata Gathering ---
$GitHash = git rev-parse --short HEAD
$IsDirty = git status --porcelain
$DirtySuffix = if ($IsDirty) { "-dirty" } else { "" }
$VersionTag = "$GitHash$DirtySuffix"

# --- Configuration ---
$AwsRegion = "us-east-1"
$RegistryUrl = "222779217717.dkr.ecr.us-east-1.amazonaws.com"
$ImageName = "websiteinstance"
$FullRegistryPath = "${RegistryUrl}/${ImageName}"

# --- Step 1: Initialize & Print Configuration ---
Write-Host "`n[1/5] $(Get-Emoji 0x2619) Initializing configuration..." -ForegroundColor Cyan

# This creates a vertical list (Multiple Rows) instead of one wide row
$ConfigData = @(
    [PSCustomObject]@{ Setting = "AWS Region";   Value = $AwsRegion }
    [PSCustomObject]@{ Setting = "Registry URL"; Value = $RegistryUrl }
    [PSCustomObject]@{ Setting = "Image Name";   Value = $ImageName }
    [PSCustomObject]@{ Setting = "Git Hash";     Value = $GitHash }
    [PSCustomObject]@{ Setting = "Workspace";    Value = if ($IsDirty) { "Dirty (Uncommitted Changes)" } else { "Clean" } }
    [PSCustomObject]@{ Setting = "Version Tag";  Value = $VersionTag }
)

$ConfigData | Format-Table -AutoSize | Out-String | Write-Host -ForegroundColor Gray

# --- Step 2: Build ---
Write-Host "`n[2/5] $(Get-Emoji 0x1F6E0) Building Docker image (linux/arm64)..." -ForegroundColor Cyan
docker buildx build --platform linux/arm64 `
    -t "${ImageName}:latest" `
    -t "${ImageName}:${VersionTag}" `
    --load `
    -f Fydar.Dev.WebApp/Dockerfile .

# --- Step 3: Tag ---
Write-Host "`n[3/5] $(Get-Emoji 0x1F3F7) Tagging images for ECR..." -ForegroundColor Cyan
docker tag "${ImageName}:latest" "${FullRegistryPath}:latest"
docker tag "${ImageName}:${VersionTag}" "${FullRegistryPath}:${VersionTag}"

# --- Step 4: Auth ---
Write-Host "`n[4/5] $(Get-Emoji 0x1F512) Logging into AWS ECR..." -ForegroundColor Cyan
aws ecr get-login-password --region $AwsRegion | docker login --username AWS --password-stdin $RegistryUrl

# --- Step 5: Push ---
Write-Host "`n[5/5] $(Get-Emoji 0x1F680) Pushing images to AWS ECR..." -ForegroundColor Cyan
docker push "${FullRegistryPath}:latest"
docker push "${FullRegistryPath}:${VersionTag}"

Write-Host "`n$(Get-Emoji 0x2705) Deployment completed successfully!" -ForegroundColor Green
