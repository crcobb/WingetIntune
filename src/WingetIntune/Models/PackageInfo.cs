﻿using WingetIntune.Extensions;
using WingetIntune.Implementations;
using WingetIntune.Models.Manifest;

namespace WingetIntune.Models;

public class PackageInfo
{
    public string? PackageIdentifier { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? Version { get; set; }
    public PackageSource Source { get; set; } = PackageSource.Unknown;
    public string? Publisher { get; set; }
    public Uri? InformationUrl { get; set; }
    public Uri? PublisherUrl { get; set; }
    public Uri? SupportUrl { get; set; }
    public InstallerType InstallerType { get; set; } = InstallerType.Unknown;
    public Uri? InstallerUrl { get; set; }
    public string? Hash { get; set; }
    public string? InstallCommandLine { get; set; }
    public string? UninstallCommandLine { get; set; }
    public string? MsiVersion { get; set; }
    public string? MsiProductCode { get; set; }
    public string? InstallerFilename { get; set; }
    public List<Manifest.WingetInstaller>? Installers { get; set; }
    public Manifest.WingetInstaller? Installer { get; set; }

    public InstallerContext? InstallerContext { get; set; }
    public Architecture? Architecture { get; set; }

    public string? DetectionScript { get; set; }

    internal WingetInstaller? GetBestFit(Architecture architecture, InstallerContext context)
    {
        if (Installers is null) { return null; }
        return Installers.SingleOrDefault(Models.InstallerType.Msi, architecture, context)
            ?? Installers.SingleOrDefault(Models.InstallerType.Msi, architecture, Models.InstallerContext.Unknown)
            ?? Installers.SingleOrDefault(Models.InstallerType.Wix, architecture, context)
            ?? Installers.SingleOrDefault(Models.InstallerType.Wix, architecture, Models.InstallerContext.Unknown)
            ?? Installers.SingleOrDefault(Models.InstallerType.Unknown, architecture, context)
            ?? Installers.SingleOrDefault(Models.InstallerType.Unknown, architecture, Models.InstallerContext.Unknown)
            ;
    }

    public static PackageInfo Parse(string wingetOutput)
    {
        var keys = wingetOutput.Trim().StartsWith(WingetManagerKeys.WingetPrefixFr) ? WingetManagerKeys.French() : WingetManagerKeys.English();

        // This should work on all platforms
        // var lines = wingetOutput.Split(Environment.NewLine) did not work on Linux
        var lines = wingetOutput.Split('\n').Select(x => x.TrimEnd('\r')).ToArray();
        var packageInfo = new PackageInfo();

        var packageIdLine = lines.FirstOrDefault(l => l.StartsWith(keys.Prefix));
        if (packageIdLine != null)
        {
            var packageIdDetails = packageIdLine.Split(" ");
            packageInfo.DisplayName = string.Join(" ", packageIdDetails.Skip(1).Take(packageIdDetails.Length - 2));
            packageInfo.PackageIdentifier = packageIdDetails[packageIdDetails.Length - 1].Trim('[', ']');
        }

        packageInfo.Version = lines.GetValue(keys.Version);
        packageInfo.Publisher = lines.GetValue(keys.Publisher);
        packageInfo.PublisherUrl = lines.GetUri(keys.PublisherUrl);
        packageInfo.InformationUrl = lines.GetUri(keys.InformationUrl);
        packageInfo.SupportUrl = lines.GetUri(keys.SupportUrl);

        packageInfo.Description = lines.GetMultiLineValue(keys.Description);


        if (wingetOutput.Contains($"{keys.InstallerType}: msstore"))
        {
            packageInfo.Source = PackageSource.Store;
        }
        else // Assume winget
        {
            packageInfo.Source = PackageSource.Winget;

            var installerType = lines.GetValueContains(keys.InstallerType);
            if (installerType != null)
            {
                packageInfo.InstallerType = EnumParsers.ParseInstallerType(installerType);
            }

            var installerUrl = lines.GetValueContains(keys.InstallerUrl);
            if (installerUrl != null)
            {
                packageInfo.InstallerUrl = new Uri(installerUrl);
                packageInfo.InstallerFilename = Path.GetFileName(packageInfo.InstallerUrl.AbsolutePath).Replace(" ", "");
            }

            packageInfo.Hash = lines.GetValueContains(keys.InstallerSha256);

        }

        return packageInfo;
    }
}
