del .\ASFAchievementManager\*.zip
dotnet publish -c "Release" -f "net472" -o "out/generic-netf"
rename .\ASFAchievementManager\ASF-Achievement-Manager.zip ASF-Achievement-Manager-netf.zip 
dotnet publish -c "Release" -f "netcoreapp2.2" -o "out/generic" "/p:LinkDuringPublish=false"