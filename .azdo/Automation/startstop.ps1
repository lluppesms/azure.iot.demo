# ---------------------------------------------------------------------------------------------------------
# For this to run right, you will have to do this first in your Powershell environment:
#  > az login
#  > az account set --subscription xxxxxx-xxxx-xxxx-xxxx-xxxxxx
# ---------------------------------------------------------------------------------------------------------

param(
  #[Parameter(Mandatory)] [ValidateSet('start','stop')] [string]$action,
  [Parameter()] [string]$action,
  [Parameter()] [string]$envName,
  [Parameter()] [string]$login
) 
$global:doLogin = $false
$global:rgName = ''
$global:streamName = ''
$global:websiteName = ''
$global:functionName = ''

# ---------------------------------------------------------------------------------------------------------
function ShowHelp {
  write-host "****************************************************************************" -ForegroundColor Green
  write-host "IoT Example Application Controller:" -ForegroundColor Green
  write-host "****************************************************************************" -ForegroundColor Green
  write-host "  Example: .\startstop.ps1 [action] [environmentName] [login]" -ForegroundColor Green
  write-host "    [action]:                     start/stop   = what action do you want to take?" -ForegroundColor Green
  write-host "    [environmentName] (optional): dev/qa/prod  = which environment do you want to affect?" -ForegroundColor Green
  write-host "    [login]: (optional)           login        = should you do an 'az Login' command first?" -ForegroundColor Green
  write-host ""
}

# ---------------------------------------------------------------------------------------------------------
function ValidateParameters {
  #if ($action -eq $null -or $action -eq "") {
  #  $action = read-host -Prompt "Please enter an action of 'start' or 'stop'" 
  #  write-host "  Action Requested: $($action)" -ForegroundColor Yellow
  #}

  Set-Variable -Name "doLogin" -value ($login -ne $null -and $login.ToLower() -eq 'login') -scope global

  Set-Variable -Name "action"  -value $action.ToLower() -scope global
  Set-Variable -Name "envName" -value $envName.ToLower() -scope global
  if ($action -eq $null -or $action -eq "") {
    write-host "  Please supply an action!! (use stop or start)" -ForegroundColor Red
  }
  else {
    if (($action -eq 'start' -or $action -eq 'stop') -ne $true) {
      write-host "  Invalid action ('$($action)') supplied!! (use stop or start)" -ForegroundColor Red
    }
  }
}
# ---------------------------------------------------------------------------------------------------------
function EchoParameters {
  write-host ""
  write-host "  Action Requested: $($action)" -ForegroundColor Yellow
  write-host "  Environment Name: $($envName)" -ForegroundColor Yellow
  write-host "  Do 'az login':    $($doLogin)" -ForegroundColor Yellow
  write-host ""
}

# ---------------------------------------------------------------------------------------------------------
function GetResourceNames {
  if ($envName -ne $null -and $envName -eq 'dev') {
    write-host "  Controlling $($envName) Environment..." -ForegroundColor Yellow
    Set-Variable -Name "rgName"       -value "rg_iotdemo_dev" -scope global
    Set-Variable -Name "streamName"   -value "lll-iotdemo-stream-dev" -scope global
    Set-Variable -Name "websiteName"  -value "lll-iotdemo-dashboard-dev" -scope global
    Set-Variable -Name "functionName" -value "lll-iotdemo-process-dev" -scope global
  }
  if ($envName -ne $null -and $envName -eq 'qa')
  {
    write-host "  Controlling $($envName) Environment..." -ForegroundColor Yellow
    Set-Variable -Name "rgName"       -value "rg_iotdemo_qa" -scope global
    Set-Variable -Name "streamName"   -value "lll-iotdemo-stream-qa" -scope global
    Set-Variable -Name "websiteName"  -value "lll-iotdemo-dashboard-qa" -scope global
    Set-Variable -Name "functionName" -value "lll-iotdemo-process-qa" -scope global
  }
  if ($rgName -eq '') {
    write-host "  Invalid environment supplied!!" -ForegroundColor Red
  }
}
# ---------------------------------------------------------------------------------------------------------
function DoAzureLogin {
  if ($doLogin) { az login }
}
# ---------------------------------------------------------------------------------------------------------
function ExecuteActions {
  if ($action -eq 'start') { StartServices }
  if ($action -eq 'stop') { StopServices }
}
# ---------------------------------------------------------------------------------------------------------
function StartServices {
  if ($rgName -ne '') {
    write-host "  Starting function $($functionName) ..."
    az functionapp start --resource-group  $rgName --name $functionName
    write-host "  Starting website $($websiteName) ..."
    az webapp start --resource-group $rgName --name $websiteName 
    write-host "  Starting stream analytics job $($streamName) ..."
	# --only-show-errors suppresses the warning that this is an experimental command group
    az stream-analytics job start  --resource-group $rgName --job-name $streamName --output-start-mode "LastOutputEventTime" --only-show-errors
  }
}

# ---------------------------------------------------------------------------------------------------------
function StopServices {
  if ($rgName -ne '') {
    write-host "  Stopping stream analytics job $($streamName) ..."
	# --only-show-errors suppresses the warning that this is an experimental command group
    az stream-analytics job stop --resource-group $rgName --job-name $streamName --only-show-errors
    write-host "  Stopping website $($websiteName) ..."
    az webapp stop --resource-group $rgName --name $websiteName 
    write-host "  Stopping function $($functionName) ..."
    az functionapp stop --resource-group  $rgName --name $functionName
  }
}
# ---------------------------------------------------------------------------------------------------------
function ShutDown {
  write-host ""
  write-host "All Done!" -ForegroundColor Yellow
  Write-Host -NoNewLine 'Press any key to continue...';
  $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown');
}
# ---------------------------------------------------------------------------------------------------------
# Main Logic
# ---------------------------------------------------------------------------------------------------------
cls
ShowHelp
ValidateParameters
GetResourceNames
EchoParameters
DoAzureLogin
ExecuteActions
ShutDown
