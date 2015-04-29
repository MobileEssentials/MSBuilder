using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Net;

namespace MSBuilder.NuGet
{
	/// <summary>
	/// Downloads a file from a URL to a destination directory 
	/// or file path.
	/// </summary>
	public class DownloadFile : Task
	{
		/// <summary>
		/// The URL to download the file from.
		/// </summary>
		[Required]
		public string SourceUrl { get; set; }

		/// <summary>
		/// If specified, it overrides the source file 
		/// name from the URL.
		/// Either DestinationFile or DestinationFolder 
		/// must be provided, but not both.
		/// </summary>
		public string DestinationFile { get; set; }

		/// <summary>
		/// The destination folder to copy the source 
		/// file to, keeping its original file name. 
		/// Either DestinationFile or DestinationFolder 
		/// must be provided, but not both.
		/// </summary>
		public string DestinationFolder { get; set; }

		/// <summary>
		/// The file that was successfully downloaded.
		/// </summary>
		[Output]
		public string DownloadedFile { get; set; }

		/// <summary>
		/// Downloads the file from the specified URL to 
		/// the destination directory or file path.
		/// </summary>
		public override bool Execute()
		{
			if (DestinationFile == null && DestinationFolder == null)
			{
				Log.LogError("No destination specified for DownloadFile. Please supply either \"DestinationFile\" or \"DestinationFolder\".");
				return false;
			}
			if (DestinationFile != null && DestinationFolder != null)
			{
				Log.LogError("Both \"DestinationFile\" and \"DestinationFolder\" were specified as input parameters in the project file. Please choose one or the other.");
				return false;
			}
			if (DestinationFile != null && Directory.Exists(DestinationFile))
			{
				Log.LogError(@"Could not download to the destination file ""{0}"", because the destination is a folder instead of a file.
 To download the source file into a folder, consider using the DestinationFolder parameter instead of DestinationFile.", DestinationFile);
				return false;
			}

			try
			{
				var request = WebRequest.CreateHttp (SourceUrl);
				var response = request.GetResponse ();

				DownloadedFile = DestinationFile;
				if (DownloadedFile == null)
					DownloadedFile = Path.Combine(DestinationFolder, Path.GetFileName(response.ResponseUri.AbsolutePath));

				var bufferSize = 4096;

				using (var webStream = response.GetResponseStream())
				using (var localStream = File.Create(DownloadedFile))
				{
					var buffer = new byte[bufferSize];
					var read = 0;
					while ((read = webStream.Read(buffer, 0, buffer.Length)) != 0)
					{
						localStream.Write(buffer, 0, read);
					}
				}
			}
			catch (Exception ex)
			{
				Log.LogErrorFromException(ex);
			}

			return !Log.HasLoggedErrors;
		}
	}
}
