rm -rf ./publish
rm -rf ./docs

dotnet publish -o ./publish -c Release 
cp -r ./publish/wwwroot ./docs