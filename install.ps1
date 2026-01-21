param(
    [string]$version = "0.1.0"
)

# build and produce nupkg (will appear in ./nupkgs)
dotnet pack -c Release src/Meow.CLI/Meow.CLI.csproj
# remove previous global install if exists
dotnet tool uninstall --global meow 
# install from local folder (tool manifest not required for global install)
dotnet tool install --global --add-source ./src/Meow.CLI/nupkgs meow --version $version
