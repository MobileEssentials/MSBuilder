using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace MSBuilder
{
	/// <summary>
	/// Injects the dependent VSIX manifest IDs and versions
	/// into the given TargetVsixManifest.
	/// </summary>
	public class AddVsixDependency : Task
	{
		/// <summary>
		/// Target VSIX manifest of the extension having VSIX dependencies.
		/// </summary>
		[Required]
		public Microsoft.Build.Framework.ITaskItem TargetVsixManifest { get; set; }

		/// <summary>
		/// VSIX manifest of the extension dependencies.
		/// </summary>
		public Microsoft.Build.Framework.ITaskItem[] VsixDependencyManifest { get; set; }

		/// <summary>
		/// Injects the dependent VSIX manifest IDs and versions
		/// into the given TargetVsixManifest.
		/// </summary>
		public override bool Execute()
		{
			if (VsixDependencyManifest == null || VsixDependencyManifest.Length == 0)
				return true;

			var xmlns = XNamespace.Get("http://schemas.microsoft.com/developer/vsx-schema/2011");
			var targetDoc = XDocument.Load(TargetVsixManifest.GetMetadata("FullPath"));
			if (targetDoc.Root.Name.Namespace != xmlns)
			{
				Log.LogError("Unsupported document root namespace {0}. Please use a VSIX manifest version 2.0.0 (xmlns='http://schemas.microsoft.com/developer/vsx-schema/2011').", targetDoc.Root.Name.NamespaceName);
				return false;
			}

            var deps = new Lazy<XElement>(() =>
            {
                var e = targetDoc.Root.Element(xmlns + "Dependencies");
                if (e == null)
                {
                    e = new XElement(xmlns + "Dependencies");
                    targetDoc.Root.Add(e);
                }

                return e;
            });
            var reqs = new Lazy<XElement>(() =>
            {
                var e = targetDoc.Root.Element(xmlns + "Prerequisites");
                if (e == null)
                {
                    e = new XElement(xmlns + "Prerequisites");
                    targetDoc.Root.Add(e);
                }

                return e;
            });

            foreach (var vsixDependency in VsixDependencyManifest)
			{
				var dependencyDoc = XDocument.Load(vsixDependency.GetMetadata("FullPath"));
				var identity = dependencyDoc.Root
					.Element(xmlns + "Metadata")
					.Element(xmlns + "Identity");
				var id = identity.Attribute("Id").Value;
				var version = identity.Attribute("Version").Value;
				var name = dependencyDoc.Root
					.Element(xmlns + "Metadata")
					.Element(xmlns + "DisplayName")
					.Value;

				var vsixPath = vsixDependency.GetMetadata("VsixPath");
				if (string.IsNullOrEmpty(vsixPath))
				{
					Log.LogError("VsixDependency '{0}' is missing required metadata 'VsixPath' for the VSIX payload.", vsixDependency.ItemSpec);
					continue;
				}

                // Dependency can be a prerequisite or a dependency, depending on the presence of 
                // the JsonPath file.
                var jsonPath = vsixDependency.GetMetadata("JsonPath");
                if (!string.IsNullOrEmpty(jsonPath))
                {
                    var componentId = Path.GetFileNameWithoutExtension(jsonPath);
                    var req = reqs.Value.Elements(xmlns + "Prerequisite").FirstOrDefault(e => e.Attribute("Id").Value == id);
                    if (req == null)
                    {
                        Log.LogMessage("Adding new prerequisite on {0} version {1}.", id, version);

                        // <Prerequisite Id="ID" Version="[VERSION,)" DisplayName="NAME" />
                        req = new XElement(xmlns + "Prerequisite",
                            new XAttribute("DisplayName", name),
                            new XAttribute("Id", componentId),
                            new XAttribute("Version", "[" + version + ",)"));

                        reqs.Value.Add(req);
                    }
                    else
                    {
                        Log.LogMessage("Updating existing prerequisite on {0} to version {1}", id, version);

                        req.Attribute("DisplayName").Value = name;
                        req.Attribute("Version").Value = "[" + version + ",)";
                    }
                }

                // If we are targetting exclusively 15+, we can't have the Dependency too. NOTE: if targeting 
                // multiple VS versions, having both Prerequisite AND dependency might not work.
                bool? shouldAddDependency = null;
                var installation = targetDoc.Root.Element(xmlns + "Installation");
                if (installation != null)
                {
                    shouldAddDependency = installation
                        .Elements(xmlns + "InstallationTarget")
                        .Select(x => x.Attribute("Version").Value)
                        .Any(v => !v.StartsWith("[15"));
                }

                if (!shouldAddDependency.HasValue || shouldAddDependency == true)
                {
                    var dependency = deps.Value.Elements(xmlns + "Dependency").FirstOrDefault(e => e.Attribute("Id").Value == id);
                    if (dependency == null)
                    {
                        Log.LogMessage("Adding new dependency on {0} version {1}.", id, version);

                        // <Dependency DisplayName="NAME" Id="ID" Version="[VERSION,)" Location="[VSIXPATH]" />
                        dependency = new XElement(xmlns + "Dependency",
                            new XAttribute("DisplayName", name),
                            new XAttribute("Id", id),
                            new XAttribute("Version", "[" + version + ",)"),
                            // NOTE: we always assume the file will be embedded as content inside the parent VSIX
                            new XAttribute("Location", Path.GetFileName(vsixPath)));

                        deps.Value.Add(dependency);
                    }
                    else
                    {
                        Log.LogMessage("Updating existing dependency on {0} to version {1}", id, version);

                        dependency.Attribute("DisplayName").Value = name;
                        dependency.Attribute("Version").Value = "[" + version + ",)";
                        dependency.Attribute("Location").Value = Path.GetFileName(vsixPath);
                    }
                }
            }

            if (Log.HasLoggedErrors)
				return false;

			targetDoc.Save(TargetVsixManifest.GetMetadata("FullPath"));

			return true;
		}
	}
}
