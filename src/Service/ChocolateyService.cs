﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stratos.Helper;
using Stratos.Model;

namespace Stratos.Service
{
	public class ChocolateyService : IChocolateyService
	{
	    private readonly string ChocolateyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "chocolatey");

        private readonly ICommandService m_command;
        private readonly IFileSystemService m_fileSystem;

        public ChocolateyService(ICommandService command, IFileSystemService fileSystem) 
		{
			m_command = command;
		    m_fileSystem = fileSystem;
		}

		public SemanticVersion ChocoVersion()
		{
			try
			{
				var version = SemanticVersion.Parse(m_command.Execute("choco", "--version", true, true));
				return version;
			}
			catch (Exception ex)
			{
				return Constants.EmptySemanticVersion;
			}
		}

		public IEnumerable<NuGetPackage> InstalledPackages()
		{
			try
			{
				return ParsePackagesOutput(m_command.Execute("choco", "list -lo -r", true, true));
			}
			catch (Exception ex) 
			{
				return new List<NuGetPackage>(); 
			}
		}

	    public IEnumerable<NuGetPackage> FailedPackages()
	    {
	        var chocolateyBadLibPath = Path.Combine(ChocolateyPath, "lib-bad");
            Console.WriteLine(chocolateyBadLibPath);
	        return !m_fileSystem.DirectoryExists(chocolateyBadLibPath)
                    ? new List<NuGetPackage>()
                    : m_fileSystem.GetDirectories(chocolateyBadLibPath).Select(p => new NuGetPackage { PackageName = Path.GetFileName(p), Version = GetPackageVersionFromNuspecFile(p)});
	    }

	    private IEnumerable<NuGetPackage> ParsePackagesOutput(string outputString) 
		{
			var packages = new List<NuGetPackage>();
			var rawPackageOutput = outputString.Split('\n');
			foreach (var textString in rawPackageOutput)
			{
                var packageSplit = textString.Split('|');
                try
                {
                    var semanticVersion = SemanticVersion.Parse(packageSplit[1]);
                    packages.Add(new NuGetPackage { PackageName = packageSplit[0], Version = new PackageVersion() { Version = semanticVersion.Version, SpecialVersion = semanticVersion.SpecialVersion}  });
                }
			    catch (Exception ex)
			    {
			        // Should logg this or something.
			    } 
			}

			return packages;
		}

		private PackageVersion GetPackageVersionFromNuspecFile(string packagePath)
	    {
	        var nuspecPath = Path.Combine(packagePath, $"{Path.GetFileName(packagePath)}.nuspec");
	        if (m_fileSystem.FileExists(nuspecPath))
	        {
	            var nuspecXml = m_fileSystem.LoadXmlDocument(nuspecPath);

	            var versionNode = nuspecXml.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='version']");

	            if (!string.IsNullOrWhiteSpace(versionNode?.InnerText))
                {
	                var semanticVersion = SemanticVersion.Parse(versionNode.InnerText);
                    return new PackageVersion { Version = semanticVersion.Version, SpecialVersion = semanticVersion.SpecialVersion };
	            }
	        }

	        return null;
	    }
	}
}
