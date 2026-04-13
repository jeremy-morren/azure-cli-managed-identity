try {
    Push-Location $PSScriptRoot -StackName "Tests"

    # See https://devblogs.microsoft.com/dotnet/testing-your-native-aot-dotnet-apps/
    dotnet publish --runtime win-x64 /p:AotMsCodeCoverageInstrumentation=true
    .\bin\Release\net8.0\win-x64\publish\AzCliManagedIdentity.Tests.exe `
        --coverage --coverage-output-format cobertura `
        --report-trx --results-directory '.\TestResults'
}
finally {
    Pop-Location -StackName "Tests"
}