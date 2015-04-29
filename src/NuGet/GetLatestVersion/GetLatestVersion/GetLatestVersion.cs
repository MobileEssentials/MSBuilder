using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace MSBuilder.NuGet
{
	/// <summary>
	/// Retrieves the latest published version of a given nuget package 
	/// from nuget.org, and provides easy access to its various SemVer 
	/// components.
	/// </summary>
	public class GetLatestVersion : Task
	{
		/// <summary>
		/// Whether to also include pre-release versions in the lookup. 
		/// Defaults to false.
		/// </summary>
		public bool IncludePreRelease { get; set; }

		/// <summary>
		/// The package identifier to retrieve the latest version for.
		/// </summary>
		[Required]
		public string PackageId { get; set; }

		/// <summary>
		/// The retrieved version, or "0.0" if no entry was found 
		/// for the given identifier.
		/// </summary>
		[Output]
		public string PackageVersion { get; set; }

		/// <summary>
		/// The simple version, containing Major.Minor.Patch components.
		/// </summary>
		[Output]
		public string SimpleVersion { get; set; }

		/// <summary>
		/// The Major component of the version.
		/// </summary>
		[Output]
		public int Major { get; set; }

		/// <summary>
		/// The Minor component of the version.
		/// </summary>
		[Output]
		public int Minor { get; set; }

		/// <summary>
		/// The Patch component of the version.
		/// </summary>
		[Output]
		public int Patch { get; set; }

		/// <summary>
		/// The optional pre-release label component of the version, including 
		/// the leading hyphen after the Patch component.
		/// </summary>
		[Output]
		public string Release { get; set; }

		/// <summary>
		/// The optional Build component of the version, including the plus
		/// sign following the Patch or PreRelease components.
		/// </summary>
		[Output]
		public string Build { get; set; }

		/// <summary>
		/// Retrieves the latest version of the given package.
		/// </summary>
		public override bool Execute()
		{
			// https://www.nuget.org/api/v2/FindPackagesById()?$filter=IsLatestVersion&id='MSBuilder.Run'
			var url = string.Format("https://www.nuget.org/api/v2/FindPackagesById()?$filter={0}&id='{1}'",
				IncludePreRelease ? "IsAbsoluteLatestVersion" : "IsLatestVersion",
				PackageId);

			var xmlns = new XmlNamespaceManager(new NameTable());
			xmlns.AddNamespace("f", "http://www.w3.org/2005/Atom");
			xmlns.AddNamespace("d", "http://schemas.microsoft.com/ado/2007/08/dataservices");
			xmlns.AddNamespace("m", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata");

			// See: https://github.com/emgarten/NuGet.Packaging/blob/master/src/Versioning/Constants.cs
			// or https://github.com/NuGet/NuGet.Versioning/blob/master/src/Versioning/Constants.cs
			var semVer = new Regex(@"^(?<Version>\d+(\s*\.\s*\d+){0,3})(?<Release>-([0]\b|[0]$|[0][0-9]*[A-Za-z-]+|[1-9A-Za-z-][0-9A-Za-z-]*)+(\.([0]\b|[0]$|[0][0-9]*[A-Za-z-]+|[1-9A-Za-z-][0-9A-Za-z-]*)+)*)?(?<Metadata>\+[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$",
				RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

			try
			{
				Log.LogMessage(MessageImportance.Low, "Retrieving latest version metadata for package '{0}'.", PackageId);

				var doc = XDocument.Load(url);
				var nav = doc.CreateNavigator(xmlns.NameTable);
				var raw = (string)nav.Evaluate("string(/f:feed/f:entry/m:properties/d:Version/text())", xmlns);

				if (raw.Length == 0)
					raw = "0.0.0";

				var match = semVer.Match(raw);

				if (!match.Success)
				{
					Log.LogError("Retrieved version '{0}' is not a valid SemVer version.", raw);
					return false;
				}

				Log.LogMessage(MessageImportance.Low, "Retrieved version '{0}' for package '{1}'.", raw, PackageId);

				PackageVersion = raw;
				SimpleVersion = match.Groups["Version"].Value;

				var version = new Version(SimpleVersion);
				Major = version.Major;
				Minor = version.Minor;
				Patch = version.Build;

				if (match.Groups["Release"].Success)
					Release = match.Groups["Release"].Value;
				if (match.Groups["Metadata"].Success)
					Release = match.Groups["Metadata"].Value;
			}
			catch (Exception e)
			{
				Log.LogErrorFromException(e);
				return false;
			}

			return true;
		}
	}
}
