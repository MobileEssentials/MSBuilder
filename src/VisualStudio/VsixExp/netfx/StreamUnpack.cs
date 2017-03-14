#region BSD License
/* 
Copyright (c) 2010, NETFx
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.

* Neither the name of Clarius Consulting nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Packaging;

/// <summary>
/// Provides unpacking behavior for steams are 
/// OPC (Open Packaging Convention) packages/zips.
/// </summary>
/// <nuget id="netfx-System.IO.Packaging.StreamUnpack" />
internal static partial class StreamUnpack
{
	/// <summary>
	/// Unzips the given stream onto the target directory.
	/// </summary>
	/// <param name="zipStream" this="true">The stream to unpack</param>
	/// <param name="targetDir">The target directory where stream will be unpacked</param>
	/// <remarks>
	/// If the <paramref name="targetDir"/> already exists, 
	/// it's deleted before unzipping begins to ensure a 
	/// clean destination folder.
	/// <para>
	/// The compressed stream must be a proper Package in term of XPS/OPC 
	/// (at a minimum, have a [Content_Types].xml).
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// using (var pkg = File.OpenRead("netfx-Guard.1.2.0.0.nupkg"))
	/// {
	/// 	pkg.Unpack("TempDir");
	/// }
	/// </code>
	/// </example>
	public static void Unpack(this Stream zipStream, string targetDir)
	{
		zipStream.Unpack(targetDir, new string[0]);
	}

	/// <summary>
	/// From the given stream, unzips the file with the given name 
	/// onto the given <paramref name="unpacked"/> stream.
	/// </summary>
	/// <param name="zipStream" this="true">The stream to unpack</param>
	/// <param name="fileToUnpack">The file inside te pack to unpack</param>\
	/// <param name="unpacked">The stream where the file will be unpacked</param>
	/// <remarks>
	/// If the <paramref name="fileToUnpack"/> is not found, nothing gets 
	/// written to <paramref name="unpacked"/>.
	/// </remarks>
	/// <returns><see langword="true"/> if the was <paramref name="fileToUnpack"/> 
	/// found and unpacked; <see langword="false"/> otherwise.</returns>
	/// <example>
	/// <code>
	/// using (var pkg = File.OpenRead("netfx-Guard.1.2.0.0.nupkg"))
	/// {
	/// 	var stream = new MemoryStream();
	/// 	var succeed = pkg.Unpack("License.txt", stream);
	/// 
	/// 	stream.Position = 0;
	/// 	var content = new StreamReader(stream).ReadToEnd();
	/// }
	/// </code>
	/// </example>
	public static bool Unpack(this Stream zipStream, string fileToUnpack, Stream unpacked)
	{
		using (var package = ZipPackage.Open(zipStream, FileMode.Open, FileAccess.Read))
		{
			var part = package.GetParts()
				.FirstOrDefault(x => GetFileName(x.Uri.OriginalString).Equals(fileToUnpack));

			if (part != null)
			{
				using (var partStream = part.GetStream())
				{
					partStream.WriteTo(unpacked);
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Unzips the selected files from the zip stream onto the target directory.
	/// </summary>
	/// <param name="zipStream" this="true">The stream to unpack</param>
	/// <param name="targetDir">The target directory where stream will be unpacked</param>
	/// <param name="filesToUnpack">The files to be unpacked</param>
	/// <remarks>
	/// If the <paramref name="targetDir"/> already exists, 
	/// it's deleted before unzipping begins to ensure a 
	/// clean destination folder.
	/// <para>
	/// The compressed stream must be a proper Package in term of XPS/OPC 
	/// (at a minimum, have a [Content_Types].xml).
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// using (var pkg = File.OpenRead("netfx-Guard.1.2.0.0.nupkg"))
	/// {
	/// 	pkg.Unpack("TempDir", "Guard.cs");
	/// }
	/// </code>
	/// </example>
	public static void Unpack(this Stream zipStream, string targetDir, params string[] filesToUnpack)
	{
		var baseDir = new DirectoryInfo(Environment.ExpandEnvironmentVariables(targetDir));
		if (!baseDir.Exists)
		{
			baseDir.Create();
		}
		else
		{
			baseDir.Delete(true);
			baseDir.Create();
		}

		using (var package = ZipPackage.Open(zipStream, FileMode.Open, FileAccess.Read))
		{
			foreach (var part in package.GetParts())
			{
				var targetFile = BuildTargetFileName(baseDir.FullName, part.Uri);

				if (ShouldUnpack(targetFile, filesToUnpack))
				{
					var dirName = Path.GetDirectoryName(Environment.ExpandEnvironmentVariables(targetFile));
					if (!baseDir.Equals(dirName))
					{
						var subDir = new DirectoryInfo(dirName);
						if (!subDir.Exists)
						{
							subDir.Create();
						}
					}

                    if (!File.Exists(targetFile))
                    {
                        using (var partStream = part.GetStream())
                        {
                            partStream.WriteTo(targetFile);
                        }
                    }
                }
			}
		}
	}

	private static bool ShouldUnpack(string filePath, string[] filesToUnpack)
	{
		if (filesToUnpack == null || filesToUnpack.Length == 0)
			return true;

		var fileName = Path.GetFileName(filePath);

		return filesToUnpack.Contains(fileName);
	}

	private static string BuildTargetFileName(string baseDir, Uri partUri)
	{
		var targetFile = GetFileName(partUri.OriginalString);

		return Path.Combine(baseDir, targetFile);
	}

	private static string GetFileName(string uri)
	{
		var targetFile = uri.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
		if (targetFile.StartsWith(Path.DirectorySeparatorChar.ToString()))
			targetFile = targetFile.Substring(1);

		return targetFile;
	}
}
