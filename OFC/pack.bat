rem pack removes all bin/obj, removes previous nupkg and .zip, then rebuilds
rem creates a .zip copy so you can check it manually - important

delete -confirm *.nupkg *.zip
delete -confirm /rp bin
delete -confirm /rp obj

nuget pack OFC.csproj -Verbosity detailed -Build -Properties Configuration=Release

copy *.nupkg *.zip

copy *.nupkg ..\..\examples\packages
