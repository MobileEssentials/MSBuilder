using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MSBuilder
{
    public class DownloadFileTests
    {
        const string xmlns = "{http://schemas.microsoft.com/developer/msbuild/2003}";
        static readonly string MSBuildPath = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSBuild\ToolsVersions\12.0", "MSBuildToolsPath", @"C:\Program Files (x86)\MSBuild\12.0\bin\");

		MockBuildEngine engine;

		public DownloadFileTests(ITestOutputHelper output)
		{
			engine = new MockBuildEngine(output, true);
		}

		[Fact]
		public void when_downloading_from_non_file_url_then_determines_file_name_from_response()
		{
			var task = new DownloadFile
			{
				BuildEngine = engine,
				DestinationFolder = Path.GetTempPath(),
				SourceUrl = "https://www.nuget.org/api/v2/package/MSBuilder/0.1.0",
			};

			Assert.True(task.Execute());

			Assert.True(Path.GetFileName(task.DownloadedFile).Equals("msbuilder.0.1.0.nupkg", StringComparison.OrdinalIgnoreCase));
		}

		[Fact]
		public void when_downloading_to_folder_then_preserves_source_file_name()
		{
			var task = new DownloadFile
			{
				BuildEngine = engine,
				DestinationFolder = Path.GetTempPath(),
				SourceUrl = "https://www.nuget.org/favicon.ico",
			};

			Assert.True(task.Execute());

			Assert.Equal("favicon.ico", Path.GetFileName(task.DownloadedFile));
		}

		[Fact]
		public void when_web_exception_happens_then_returns_false_and_logs_exception()
		{
			var task = new DownloadFile
			{
				BuildEngine = engine,
				DestinationFolder = Path.GetTempPath(),
				SourceUrl = "https://www.foo.bar/favicon.ico",
			};

			Assert.False(task.Execute());
			Assert.Equal(1, engine.LoggedErrorEvents.Count);
		}

		[Fact]
		public void when_no_destination_then_returns_false_and_logs_error()
		{
			var task = new DownloadFile
			{
				BuildEngine = engine,
				SourceUrl = "https://www.foo.bar/favicon.ico",
			};

			Assert.False(task.Execute());
			Assert.Equal(1, engine.LoggedErrorEvents.Count);
		}

		[Fact]
		public void when_both_destinations_then_returns_false_and_logs_error()
		{
			var task = new DownloadFile
			{
				BuildEngine = engine,
				DestinationFolder = Path.GetTempPath(),
				DestinationFile = Path.GetTempFileName(),
				SourceUrl = "https://www.foo.bar/favicon.ico",
			};

			Assert.False(task.Execute());
			Assert.Equal(1, engine.LoggedErrorEvents.Count);
		}

		[Fact]
		public void when_destination_file_is_folder_then_returns_false_and_logs_error()
		{
			var task = new DownloadFile
			{
				BuildEngine = engine,
				DestinationFile = Path.GetTempPath(),
				SourceUrl = "https://www.foo.bar/favicon.ico",
			};

			Assert.False(task.Execute());
			Assert.Equal(1, engine.LoggedErrorEvents.Count);
		}

		[Fact]
		public void when_downloading_file_then_file_is_same_from_direct_download()
		{
			var expected = Path.GetTempFileName();
			new WebClient().DownloadFile("https://www.nuget.org/api/v2/package/MSBuilder/0.1.0", expected);

			var task = new DownloadFile
			{
				BuildEngine = engine,
				Overwrite = true,
				DestinationFile = Path.GetTempFileName(),
				SourceUrl = "https://www.nuget.org/api/v2/package/MSBuilder/0.1.0",
			};

			Assert.True(task.Execute());
			Assert.Equal(File.ReadAllBytes(expected), File.ReadAllBytes(task.DownloadedFile));
		}

		[Fact]
		public void when_destination_file_exists_and_not_overwrite_then_skips_download()
		{
			var expected = Path.GetTempFileName();

			var task = new DownloadFile
			{
				BuildEngine = engine,
				DestinationFile = Path.GetTempFileName(),
				Overwrite = false,
				SourceUrl = "https://www.foo.bar/baz",
			};

			Assert.True(task.Execute());
		}
    }
}
