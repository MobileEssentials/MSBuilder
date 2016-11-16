using System;
using System.IO;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Win32;
using System.Linq;

namespace MSBuilder
{
	/// <summary>
	/// Enables the extension with the given identifier in the 
	/// given Visual Studio version and optional hive/instance (i.e. 'Exp').
	/// </summary>
	public class EnableVsix : Task
	{
		/// <summary>
		/// Visual Studio version to enable the VSIX for.
		/// </summary>
		[Required]
		public string VisualStudioVersion { get; set; }

		/// <summary>
		/// Identifier of the extension to enable.
		/// </summary>
		[Required]
		public string VsixId { get; set; }

        /// <summary>
        /// Optional value set when building from MSBuild 15 or VS 2017+
        /// </summary>
        public string VsInstallRoot { get; set; }

        /// <summary>
        /// Optional message importance for the task messages.
        /// </summary>
        public string MessageImportance { get; set; }

		/// <summary>
		/// Optional flag to fail if the extension is not already installed.
		/// </summary>
		public bool FailIfNotInstalled { get; set; }

		/// <summary>
		/// Optional hive/instance to enable in (i.e. 'Exp').
		/// </summary>
		public string RootSuffix { get; set; }

        /// <summary>
        /// Enables the extension in the given 
        /// Visual Studio version and instance/hive.
        /// </summary>
        public override bool Execute()
        {
            string vsdir = VsInstallRoot;
            var vsversion = "Visual Studio " + VisualStudioVersion;
            if (!string.IsNullOrEmpty(RootSuffix))
                vsversion += " (" + RootSuffix + ")";

            var importance = Microsoft.Build.Framework.MessageImportance.Normal;
            if (!string.IsNullOrEmpty(MessageImportance))
                importance = (MessageImportance)Enum.Parse(typeof(MessageImportance), MessageImportance, true);

            object settings = null;
            object manager = null;
            Type managerType = null;

            // Actual task implementation run afterwards.
            Func<bool> execute = () =>
            {
                try
                {
                    var extension = managerType.InvokeMember("GetInstalledExtension", BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod, null, manager, new[] { VsixId });
                    var state = (int)extension.GetType().InvokeMember("State", BindingFlags.GetProperty, null, extension, null);
                    if (state != 1)
                    {
                        Log.LogMessage(importance, "Enabling '{0}' on {1}.", VsixId, vsversion);
                        managerType.InvokeMember("Enable", BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod, null, manager, new[] { extension });
                        managerType.InvokeMember("UpdateLastExtensionsChange", BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod, null, manager, new object[0]);
                        Log.LogMessage(importance, "Successfully enabled extension '{0}' on {1}.", VsixId, vsversion);
                    }
                    else
                    {
                        Log.LogMessage(importance, "Extension '{0}' was already enabled on {1}.", VsixId, vsversion);
                    }

                    return true;
                }
                catch (TargetInvocationException tie)
                {
                    if (tie.InnerException.GetType().FullName == "Microsoft.VisualStudio.ExtensionManager.NotInstalledException")
                    {
                        if (FailIfNotInstalled)
                        {
                            Log.LogError("Extension '{0}' is not installed on {1}.", VsixId, vsversion);
                            return false;
                        }
                        else
                        {
                            Log.LogMessage(importance, "Extension '{0}' is not installed on {1}.", VsixId, vsversion);
                        }
                    }
                    else
                    {
                        Log.LogErrorFromException(tie.InnerException, true);
                    }

                    return false;
                }
            };

            #region Execute

            if (string.IsNullOrEmpty(vsdir))
            {
                using (var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                using (var key = root.OpenSubKey(@"Software\Microsoft\VisualStudio\SxS\VS7"))
                {
                    if (key != null)
                        vsdir = key.GetValue(VisualStudioVersion) as string;

                    if (string.IsNullOrEmpty(vsdir))
                    {
                        Log.LogError("Failed to locate installation directory for VisualStudioVersion '{0}'.", VisualStudioVersion);
                        return false;
                    }
                }
            }

            var asmFile = Path.Combine(vsdir, @"Common7\IDE\PrivateAssemblies\Microsoft.VisualStudio.ExtensionManager.Implementation.dll");
            if (!File.Exists(asmFile))
            {
                Log.LogError(string.Format("Failed to locate extension manager implementation at '{0}'.", asmFile));
                return false;
            }

            var vssdk = new DirectoryInfo(Path.Combine(vsdir, @"VSSDK\VisualStudioIntegration\Common\Assemblies\v4.0")).FullName;
            if (!Directory.Exists(vssdk))
            {
                Log.LogError(string.Format("Visual Studio SDK was not found at expected path '{0}'.", vssdk));
                return false;
            }

            var settingsFile = Path.Combine(vssdk, string.Format("Microsoft.VisualStudio.Settings.{0}.dll", VisualStudioVersion));
            if (!File.Exists(settingsFile))
            {
                Log.LogError(string.Format("Failed to locate settings manager implementation at '{0}'.", settingsFile));
                return false;
            }

            ResolveEventHandler resolver = (sender, args) =>
            {
                var requestedName = new AssemblyName(args.Name).Name;
                var requestedFile = Path.Combine(vsdir, requestedName + ".dll");
                if (!File.Exists(requestedFile))
                    requestedFile = Path.Combine(vsdir, @"Common7\IDE\" + requestedName + ".dll");
                if (!File.Exists(requestedFile))
                    requestedFile = Path.Combine(vsdir, @"Common7\IDE\PrivateAssemblies\" + requestedName + ".dll");
                if (!File.Exists(requestedFile))
                    requestedFile = Path.Combine(vsdir, @"Common7\IDE\PublicAssemblies\" + requestedName + ".dll");
                if (!File.Exists(requestedFile))
                    requestedFile = Path.Combine(vsdir, @"VSSDK\VisualStudioIntegration\Common\Assemblies\v4.0\" + requestedName + ".dll");

                if (File.Exists(requestedFile))
                {
                    try
                    {
                        return Assembly.LoadFrom(requestedFile);
                    }
                    catch
                    {
                        return null;
                    }
                }

                return null;
            };

            AppDomain.CurrentDomain.AssemblyResolve += resolver;

            try
            {
                var managerAsm = Assembly.LoadFrom(asmFile);
                var settingsAsm = Assembly.LoadFrom(settingsFile);
                var settingsType = settingsAsm.GetType("Microsoft.VisualStudio.Settings.ExternalSettingsManager");

                settings = settingsType.InvokeMember("CreateForApplication", BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod, null, null,
                    new[] { Path.Combine(vsdir, @"Common7\IDE\devenv.exe"), RootSuffix ?? "" });

                managerType = managerAsm.GetType("Microsoft.VisualStudio.ExtensionManager.ExtensionManagerService", true);
                manager = Activator.CreateInstance(managerType, new[] { settings });

                return execute();
            }
            catch (TargetInvocationException tie)
            {
                Log.LogErrorFromException(tie.InnerException, true);
                return false;
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex, true);
                return false;
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= resolver;
            }

            #endregion

            return true;
        }
    }
}
