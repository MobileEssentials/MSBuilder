using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using System.IO.Packaging;
using System.Net.Mime;
using System.Globalization;
using System.Diagnostics;

namespace VsixExp
{
    class Program
    {
        static readonly ITracer tracer = Tracer.Get("*");
        static readonly Version MinVsixVersion = new Version("2.0.0");

        static int Main(string[] args)
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

        static bool Experimentalize(string sourceVsixFile, string targetVsixFile = null)
        {
            var temp = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(sourceVsixFile));
            if (!Directory.Exists(temp))
                Directory.CreateDirectory(temp);

            tracer.Info($"Extracting {Path.GetFileName(sourceVsixFile)}...");
            using (var stream = File.OpenRead(sourceVsixFile))
            {
                stream.Unpack(temp);
            }

            var manifest = XDocument.Load(Path.Combine(temp, "extension.vsixmanifest")).Root.ToDynamic();

            var vsixVersion = new Version((string)manifest["Version"]);
            if (vsixVersion < MinVsixVersion)
            {
                tracer.Warn($"VSIX {Path.GetFileName(sourceVsixFile)} has a manifest version lower than v{MinVsixVersion}, which is not unsupported.");
                return false;
            }

            if (targetVsixFile == null)
                targetVsixFile = sourceVsixFile;

            // Mark VSIX as experimental.
            var experimental = manifest.Installation["Experimental"];
            if (experimental == null)
            {
                experimental = manifest.Installation["Experimental"] = new XAttribute("Experimental", "true");
            }
            else if (((XAttribute)experimental).Value.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                tracer.Warn($"VSIX {Path.GetFileName(sourceVsixFile)} is already an experimental VSIX and will be skipped.");
                // Only copy over if the source is newer than the target in this case.
                if (sourceVsixFile != targetVsixFile && File.GetLastWriteTime(sourceVsixFile) > File.GetLastWriteTime(targetVsixFile))
                    File.Copy(sourceVsixFile, targetVsixFile, true);

                return true;
            }
            else
            {
                ((XAttribute)experimental).SetValue("true");
            } 

            // Update the source manifest
            ((XElement)manifest).Document.Save(Path.Combine(temp, "extension.vsixmanifest"));

            var vsixId = (string)manifest.Metadata.Identity["Id"];
            var packageId = (string)manifest.Metadata.PackageId;

            dynamic package = new JObject();
            package.id = "Component." + (packageId ?? vsixId);
            package.version = (string)manifest.Metadata.Identity["Version"];
            package.type = "Component";
            package.extension = true;
            package.dependencies = new JObject();

            var dependencies = new JObject
            {
                // The VSIX itself is the first dependency declared always.
                [packageId ?? vsixId] = package.version
            };

            if (manifest.Prerequisites != null)
            {
                // Grab dependencies
                foreach (var dependency in manifest.Prerequisites)
                {
                    var version = NuGet.Versioning.VersionRange.Parse((string)dependency["Version"]);

                    dependencies[(string)dependency["Id"]] = (version.HasLowerBound ? version.MinVersion.ToString() : version.MaxVersion.ToString());
                }
            }

            package.dependencies = dependencies;
            package.localizedResources = new JArray(new JObject
            {
                ["language"] = (string)manifest.Metadata.Identity["Language"],
                ["title"] = (string)manifest.Metadata.DisplayName,
                ["description"] = (string)manifest.Metadata.Description,
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
            using (var vsixPackage = ZipPackage.Open(targetVsixFile, FileMode.Create))
            {
                foreach (var file in Directory.GetFiles(temp, "*.*", SearchOption.AllDirectories).Where(f => !f.StartsWith(rels) && !f.StartsWith(pkg)))
                {
                    tracer.Verbose($"Packing {file.Substring(temp.Length + 1)}...");
                    var uri = PackUriHelper.CreatePartUri(new Uri(file.Substring(temp.Length + 1), UriKind.Relative));
                    var part = vsixPackage.CreatePart(uri, GetMimeTypeFromExtension(Path.GetExtension(file)), CompressionOption.Normal);
                    using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        stream.CopyTo(part.GetStream());
                    }
                }

                vsixPackage.Close();
            }

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