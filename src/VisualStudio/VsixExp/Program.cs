using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Xml.Linq;
using System.Linq;
using System.Net.Mime;
using System.Globalization;
using System.Diagnostics;
using System.Configuration;
using System.IO.Compression;
using System.Threading;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace VsixExp
{
    class Program
    {
        /// <summary>
        /// XML namespace of a VSIX extension manifest.
        /// </summary>
        public static XNamespace XmlNs => XNamespace.Get("http://schemas.microsoft.com/developer/vsx-schema/2011");

        /// <summary>
        /// If the InstallationTargetVersion is specified in AppSettings, but no InstallationTargetId 
        /// is specified, use this value as the default target.
        /// </summary>
        public const string DefaultInstallationTargetId = "Microsoft.VisualStudio.Community";

        static readonly ITracer tracer = Tracer.Get("*");
        static readonly Version MinVsixVersion = new Version("2.0.0");

        static int Main(string[] args)
        {
            try
            {
                if (!Tracer.Configuration.GetSource("*").Listeners.OfType<ConsoleTraceListener>().Any())
                {
                    Tracer.Configuration.AddListener("*", new ConsoleTraceListener());
                    Tracer.Configuration.SetTracingLevel("*", SourceLevels.Information);
                }

                var result = true;

                if (args.Length == 0)
                {
                    foreach (var vsixFile in Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.vsix", SearchOption.TopDirectoryOnly))
                    {
                        result &= Experimentalize(vsixFile);
                    }
                }
                else if (args.Length == 1)
                {
                    result = Experimentalize(args[0]);
                }
                else if (args.Length == 2)
                {
                    result = Experimentalize(args[0], args[1]);
                }
                else
                {
                    Console.WriteLine("Usage: VsixExp.exe [SourceVsix] [TargetVsix]");
                    Console.WriteLine("If no VSIX arguments are provided, all VSIXes found in the current directory will be processed and experimentalized in-place.");
                    return -1;
                }

                Console.WriteLine("Done. Double-click VSIXes to install.");
                return result ? 0 : -1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return -1;
            }
        }

        static bool Experimentalize(string sourceVsixFile, string targetVsixFile = null)
        {
            var temp = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(sourceVsixFile));
            if (Directory.Exists(temp) && !Debugger.IsAttached)
                Directory.Delete(temp, true);

            Directory.CreateDirectory(temp);

            var manifestFile = Path.Combine(temp, "extension.vsixmanifest");
            var catalogFile = Path.Combine(temp, "catalog.json");

            tracer.Info($"Processing {Path.GetFileName(sourceVsixFile)}...");

            using (var zipFile = ZipFile.OpenRead(sourceVsixFile))
            {
                var manifestEntry = zipFile.GetEntry("extension.vsixmanifest");
                if (File.Exists(manifestFile))
                    File.Delete(manifestFile);

                var retryStrategy = new ExponentialBackoff(5, TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(20));
                var retryPolicy = new RetryPolicy(DetectionStrategy.Create(ex => ex is DirectoryNotFoundException), retryStrategy);

                retryPolicy.ExecuteAction(() =>
                {
                    Directory.CreateDirectory(temp);
                    manifestEntry.ExtractToFile(manifestFile);
                });

                var catalogEntry = zipFile.GetEntry("catalog.json");
                if (catalogEntry != null)
                {
                    if (File.Exists(catalogFile))
                        File.Delete(catalogFile);

                    retryPolicy.ExecuteAction(() =>
                    {
                        Directory.CreateDirectory(temp);
                        catalogEntry.ExtractToFile(catalogFile);
                    });
                }
            }

            var manifest = XDocument.Load(manifestFile).Root;

            var vsixVersion = new Version(manifest.Attribute("Version").Value);
            if (vsixVersion < MinVsixVersion)
            {
                tracer.Warn($"VSIX {Path.GetFileName(sourceVsixFile)} has a manifest version lower than v{MinVsixVersion}, which is not unsupported.");
                return false;
            }

            if (targetVsixFile == null)
                targetVsixFile = sourceVsixFile;

            var metadata = manifest.Element(XmlNs + "Metadata");
            var identity = metadata.Element(XmlNs + "Identity");

            // Mark VSIX as experimental.
            var installation = manifest.Element(XmlNs + "Installation");
            if (installation == null)
            {
                installation = new XElement(XmlNs + "Installation");
                metadata.AddAfterSelf(installation);
            }

            // Override extension's installation targets if specified via config.
            var targetVersion = ConfigurationManager.AppSettings["InstallationTargetVersion"];
            var targetId = ConfigurationManager.AppSettings["InstallationTargetId"] ?? DefaultInstallationTargetId;
            if (!string.IsNullOrEmpty(targetVersion))
            {
                installation.RemoveNodes();
                installation.Add(new XElement(XmlNs + "InstallationTarget", new XAttribute("Id", targetId), new XAttribute("Version", targetVersion)));
            }

            var experimental = installation.Attribute("Experimental");
            if (experimental == null)
            {
                experimental = new XAttribute("Experimental", "true");
                installation.Add(experimental);
            }

            experimental.SetValue("true");

            // Update the source manifest
            manifest.Document.Save(Path.Combine(temp, "extension.vsixmanifest"));

            var vsixId = identity.Attribute("Id").Value;
            var packageId = metadata.Element(XmlNs + "PackageId")?.Value;

            dynamic package = new JObject();
            package.id = "Component." + (packageId ?? vsixId);
            package.version = identity.Attribute("Version").Value;
            package.type = "Component";
            package.extension = true;
            package.dependencies = new JObject();

            var dependencies = new JObject
            {
                // The VSIX itself is the first dependency declared always.
                [packageId ?? vsixId] = package.version
            };

            var prereqs = manifest.Element(XmlNs + "Prerequisites");
            if (prereqs != null)
            {
                // Grab dependencies
                foreach (var prereq in prereqs.Elements(XmlNs + "Prerequisite"))
                {
                    var version = NuGet.Versioning.VersionRange.Parse(prereq.Attribute("Version").Value);

                    dependencies[prereq.Attribute("Id").Value] = (version.HasLowerBound ? version.MinVersion.ToString() : version.MaxVersion.ToString());
                }
            }

            package.dependencies = dependencies;
            package.localizedResources = new JArray(new JObject
            {
                ["language"] = identity.Attribute("Language").Value,
                ["title"] = metadata.Element(XmlNs + "DisplayName").Value,
                ["description"] = metadata.Element(XmlNs + "Description").Value,
            });

            tracer.Verbose($"Updating VSIX contents...");
            if (sourceVsixFile != targetVsixFile)
                File.Copy(sourceVsixFile, targetVsixFile, true);

            using (var vsixPackage = ZipPackage.Open(targetVsixFile, FileMode.Open))
            {
                var uri = PackUriHelper.CreatePartUri(new Uri("extension.vsixmanifest", UriKind.Relative));
                var manifestPart = vsixPackage.GetPart(uri);
                using (var stream = manifestPart.GetStream(FileMode.Create))
                using (var input = File.OpenRead(manifestFile))
                {
                    tracer.Verbose($"Updating extension.vsixmanifest in VSIX...");
                    input.CopyTo(stream);
                }

                if (File.Exists(catalogFile))
                {
                    dynamic catalog = JObject.Parse(File.ReadAllText(catalogFile));
                    catalog.packages = new JArray(new[] { package }.Concat((IEnumerable<object>)catalog.packages).ToArray());

                    tracer.Verbose($"Writing catalog.json...");
                    File.WriteAllText(catalogFile, ((JObject)catalog).ToString(Newtonsoft.Json.Formatting.Indented));

                    var catalogUri = PackUriHelper.CreatePartUri(new Uri("catalog.json", UriKind.Relative));
                    var catalogPart = vsixPackage.GetPart(catalogUri);
                    using (var stream = catalogPart.GetStream(FileMode.Create))
                    using (var writer = new StreamWriter(stream))
                    {
                        tracer.Verbose($"Updating catalog.json in VSIX...");
                        writer.Write(((JObject)catalog).ToString(Newtonsoft.Json.Formatting.Indented));
                    }
                }
                else
                {
                    tracer.Warn($"VSIX {Path.GetFileName(sourceVsixFile)} is not a VSIX v3 since it lacks a catalog.json.");
                }

                vsixPackage.Flush();
                vsixPackage.Close();
            }

            tracer.Info($"Done writing final VSIX to {targetVsixFile}.");
            return true;
        }

        class DetectionStrategy : ITransientErrorDetectionStrategy
        {
            public static ITransientErrorDetectionStrategy Create(Func<Exception, bool> isTransient)
                => new DetectionStrategy(isTransient);

            Func<Exception, bool> isTransient;

            DetectionStrategy(Func<Exception, bool> isTransient) => this.isTransient = isTransient;

            public bool IsTransient(Exception ex) => isTransient(ex);
        }
    }
}
