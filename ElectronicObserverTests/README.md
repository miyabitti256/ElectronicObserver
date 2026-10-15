# ElectronicObserverTests

ElectronicObserver 本体の自動テストプロジェクトです (xUnit / .NET Framework 4.8)。

## 実行方法

本体は旧形式 csproj + `packages.config` で、さらに参照チェーンに含まれる
`DockPanelSuite/WinFormsUI` が .NET Framework 2.0 をターゲットにしています。
SDK 版 MSBuild (`dotnet build` / `dotnet test`) には v2.0 のターゲティングパックが無いため、
**`dotnet test` は参照チェーンのビルドで失敗します**。
Visual Studio 付属の MSBuild でビルドし、`vstest.console` で実行してください。

```powershell
# 1) NuGet パッケージの復元 (初回のみ)
.\nuget.exe restore ElectronicObserver.sln

# 2) ビルド
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
    ElectronicObserverTests\ElectronicObserverTests.csproj /p:Configuration=Debug

# 3) 実行
& "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\Extensions\TestPlatform\vstest.console.exe" `
    ElectronicObserverTests\bin\Debug\net48\ElectronicObserverTests.dll
```

Visual Studio からは通常どおりテスト エクスプローラーで実行できます。

## 方針

- まずは UI や `KCDatabase` に依存しない純ロジックを対象にします。
- `KCDatabase` や艦隊データが必要なテストは、記録した API 応答を再生する方式で追加していきます。
