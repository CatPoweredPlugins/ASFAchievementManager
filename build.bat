del .\ASFAchievementManager\*.zip
dotnet publish -c "Release" -f "net48" -o "out/generic-netf"
rename .\ASFAchievementManager\ASF-Achievement-Manager.zip ASF-Achievement-Manager-netf.zip
dotnet publish -c "Release" -f "netcoreapp3.0" -o "out/generic" "/p:LinkDuringPublish=false"
