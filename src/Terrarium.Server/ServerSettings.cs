namespace Terrarium.Server;

/// <summary>
/// Configuration settings for the Terrarium server, bound via IOptions from the "Terrarium" config section.
/// Ported from the legacy Server/Website/App_Code/Code/ServerSettings.cs static properties.
/// </summary>
public sealed class ServerSettings
{
    public string WelcomeMessage { get; set; } = "Welcome to .NET Terrarium 2.0!";
    public string MOTD { get; set; } = "Have Fun!";
    public string LatestVersion { get; set; } = "1.0.0.0";
    public string SpeciesDsn { get; set; } = string.Empty;
    public string AssemblyPath { get; set; } = string.Empty;
    public string InstallRoot { get; set; } = string.Empty;
    public string ChartPath { get; set; } = string.Empty;
    public string ChartUrl { get; set; } = "~/chartdata";
    public string WordListFile { get; set; } = string.Empty;
    public int MillisecondsToRollupData { get; set; } = 450_000;
    public int IntroductionWait { get; set; } = 5;
    public int IntroductionDailyLimit { get; set; } = 30;
}
