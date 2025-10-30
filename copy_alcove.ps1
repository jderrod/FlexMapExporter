$source = "C:\Users\james.derrod\Desktop\FlexMapTest1\Alcove Recessed"
$destination = "C:\Users\james.derrod\Configurator\WebConfigurator_Alcove\AlcoveData"

Copy-Item -Path $source -Destination $destination -Recurse -Force
Write-Host "Alcove data copied successfully!"
