rem pack removes all bin/obj, removes previous nupkg and .zip, then rebuilds
rem creates a .zip copy so you can check it manually - important

del -confirm *.nupkg *.zip
del -confirm /rp bin
del -confirm /rp obj

nuget pack OFC.csproj -Verbosity detailed -Build -Properties Configuration=Release

copy *.nupkg *.zip

