param(
  [Parameter(Mandatory=$true)][ValidateSet("ef","alias")] [string]$cmd,
  [string]$action,
  [string]$module,
  [string]$name,
  [string]$configuration = "Debug",
  [string]$from = "",
  [string]$to = "",
  [switch]$idempotent
)

$ErrorActionPreference = "Stop"
$ef = Join-Path $PSScriptRoot "deploy\scripts\ef.ps1"

switch ($cmd) {
  "ef" {
    $params = @{
      Action        = $action
      Module        = $module
      Name          = $name
      Configuration = $configuration
    }
    if ($from)       { $params["From"] = $from }
    if ($to)         { $params["To"] = $to }
    if ($idempotent) { $params["Idempotent"] = $true }
    & $ef @params
  }
  "alias" {
    function ef { param([Parameter(ValueFromRemainingArguments=$true)][string[]]$args) & $ef @args }

    function ef-iam-add     { param([Parameter(Mandatory=$true)]$Name) ef add iam -Name $Name }
    function ef-doc-add     { param([Parameter(Mandatory=$true)]$Name) ef add document -Name $Name }
    function ef-file-add    { param([Parameter(Mandatory=$true)]$Name) ef add file -Name $Name }

    function ef-iam-update  { ef update iam }
    function ef-doc-update  { ef update document }
    function ef-file-update { ef update file }
    function ef-update-all  { ef update all }

    function ef-iam-rollback { param($Target="0") ef rollback iam -Name $Target }
    function ef-doc-rollback { param($Target="0") ef rollback document -Name $Target }
    function ef-file-rollback{ param($Target="0") ef rollback file -Name $Target }
    function ef-rollback-all { param($Target="0") ef rollback all -Name $Target }

    function ef-iam-script  { param([string]$From="", [string]$To="", [switch]$Idempotent) 
      $args=@("script","iam"); if($From){$args+=@("-From",$From)}; if($To){$args+=@("-To",$To)}; if($Idempotent){$args+="-Idempotent"}; ef @args }
    function ef-doc-script  { param([string]$From="", [string]$To="", [switch]$Idempotent) 
      $args=@("script","document"); if($From){$args+=@("-From",$From)}; if($To){$args+=@("-To",$To)}; if($Idempotent){$args+="-Idempotent"}; ef @args }
    function ef-file-script { param([string]$From="", [string]$To="", [switch]$Idempotent) 
      $args=@("script","file"); if($From){$args+=@("-From",$From)}; if($To){$args+=@("-To",$To)}; if($Idempotent){$args+="-Idempotent"}; ef @args }
    function ef-script-all  { param([string]$From="", [string]$To="", [switch]$Idempotent)
      $args=@("script","all"); if($From){$args+=@("-From",$From)}; if($To){$args+=@("-To",$To)}; if($Idempotent){$args+="-Idempotent"}; ef @args }

    function ef-list        { ef list }
    function ef-miglist-iam { ef miglist iam }
    function ef-miglist-doc { ef miglist document }
    function ef-miglist-file{ ef miglist file }
    function ef-miglist-all { ef miglist all }

    function ef-diag-iam    { ef diag iam }
    function ef-diag-doc    { ef diag document }
    function ef-diag-file   { ef diag file }
    function ef-diag-all    { ef diag all }
    function ef-scan-all    { ef scan all }
    function ef-scan-iam    { ef scan iam }
    function ef-scan-doc    { ef scan document }
    function ef-scan-file   { ef scan file }

    Write-Host "✅ Aliases loaded for this session."
  }
}
