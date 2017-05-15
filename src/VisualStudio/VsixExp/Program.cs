using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using System.Linq;
using System.Net.Mime;
using System.Globalization;
using System.Diagnostics;
using System.Configuration;

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
            if (Directory.Exists(temp))
                Directory.Delete(temp, true);

            Directory.CreateDirectory(temp);

            tracer.Info($"Extracting {Path.GetFileName(sourceVsixFile)}...");
            var completed = 0;
            ProgressZipFile.ExtractToDirectory(sourceVsixFile, temp, new Progress<double>(d => 
            {
                if ((int)Math.Round(d * 100) > completed)
                {
                    completed = (int)Math.Round(d * 100);
                    tracer.Info($"{completed}% extraction complete");
                }
            }));

            var manifest = XDocument.Load(Path.Combine(temp, "extension.vsixmanifest")).Root;

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
            else if (experimental.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                tracer.Warn($"VSIX {Path.GetFileName(sourceVsixFile)} is already an experimental VSIX and will be skipped.");
                // Only copy over if the source is newer than the target in this case.
                if (sourceVsixFile != targetVsixFile && File.GetLastWriteTime(sourceVsixFile) > File.GetLastWriteTime(targetVsixFile))
                    File.Copy(sourceVsixFile, targetVsixFile, true);

                return true;
            }
            else
            {
                experimental.SetValue("true");
            } 

            // Update the source manifest
            manifest.Document.Save(Path.Combine(temp, "extension.vsixmanifest"));

            var vsixId = identity.Attribute("Id").Value;
            var packageId = identity.Attribute("PackageId")?.Value;

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

            if (File.Exists(Path.Combine(temp, "catalog.json")))
            {
                dynamic catalog = JObject.Parse(File.ReadAllText(Path.Combine(temp, "catalog.json")));
                catalog.packages = new JArray(new[] { package }.Concat((IEnumerable<object>)catalog.packages).ToArray());

                File.WriteAllText(Path.Combine(temp, "catalog.json"), ((JObject)catalog).ToString(Newtonsoft.Json.Formatting.Indented));
            }
            else
            {
                tracer.Warn($"VSIX {Path.GetFileName(sourceVsixFile)} is not a VSIX v3 since it lacks a catalog.json.");
            }
            
            var rels = Path.Combine(temp, "_rels");
            var pkg = Path.Combine(temp, "package");

            tracer.Info($"Compressing {Path.GetFileName(targetVsixFile)}...");
            if (File.Exists(targetVsixFile))
                File.Delete(targetVsixFile);

            completed = 0;
            ProgressZipFile.CreateFromDirectory(temp, targetVsixFile, new Progress<double>(d =>
            {
                if ((int)Math.Round(d * 100) > completed)
                {
                    completed = (int)Math.Round(d * 100);
                    tracer.Info($"{completed}% compression complete");
                }
            }));

            tracer.Info($"Done writing final VSIX to {targetVsixFile}.");
            return true;
        }

        static string GetMimeTypeFromExtension(string extension)
        {
            switch (extension.ToLower(CultureInfo.InvariantCulture))
            {
                case KnownFileExtensions.Json:
                    return "application/json";
                case KnownFileExtensions.Txt:
                case KnownFileExtensions.Pkgdef:
                    return MediaTypeNames.Text.Plain;
                case KnownFileExtensions.VsixManifest:
                case KnownFileExtensions.Xml:
                    return MediaTypeNames.Text.Xml;
                case KnownFileExtensions.Htm:
                case KnownFileExtensions.Html:
                    return MediaTypeNames.Text.Html;
                case KnownFileExtensions.Pdf:
                    return MediaTypeNames.Application.Pdf;
                case KnownFileExtensions.Rtf:
                    return MediaTypeNames.Text.RichText;
                case KnownFileExtensions.Gif:
                    return MediaTypeNames.Image.Gif;
                case KnownFileExtensions.Jpg:
                case KnownFileExtensions.Jpeg:
                    return MediaTypeNames.Image.Jpeg;
                case KnownFileExtensions.Tiff:
                    return MediaTypeNames.Image.Tiff;
                case KnownFileExtensions.Vsix:
                case KnownFileExtensions.Zip:
                    return MediaTypeNames.Application.Zip;
                default:
                    return MediaTypeNames.Application.Octet;
            }
        }

        class KnownFileExtensions
        {
            internal const string VsixManifest = ".vsixmanifest";
            internal const string Xml = ".xml";
            internal const string Txt = ".txt";
            internal const string Json = ".json";
            internal const string Pkgdef = ".pkgdef";
            internal const string Pdf = ".pdf";
            internal const string Htm = ".htm";
            internal const string Html = ".html";
            internal const string Rtf = ".rtf";
            internal const string Vsix = ".vsix";
            internal const string Zip = ".zip";
            internal const string Jpg = ".jpg";
            internal const string Jpeg = ".jpeg";
            internal const string Gif = ".gif";
            internal const string Tiff = ".tiff";
        }
    }
}