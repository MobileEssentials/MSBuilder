using System.IO;

namespace MSBuilder
{
	internal static class ModuleInitializer
	{
		public static string BaseDirectory { get; private set; }

		internal static void Run()
		{
			BaseDirectory = Directory.GetCurrentDirectory();
		}
	}
}
