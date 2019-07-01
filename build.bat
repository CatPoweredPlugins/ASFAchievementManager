del .\ASF-ChatHack\*.zip
dotnet publish -c "Release" -f "net472" -o "out/generic-netf"
rename .\ASF-ChatHack\ASF-ChatHack.zip ASF-ChatHack-netf.zip 
dotnet publish -c "Release" -f "netcoreapp2.2" -o "out/generic" "/p:LinkDuringPublish=false"