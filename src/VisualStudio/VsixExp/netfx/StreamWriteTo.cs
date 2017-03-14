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

/// <summary>
/// Provides WriteTo extension methods to write streams easily to 
/// another steam or a target file.
/// </summary>
/// <nuget id="netfx-System.IO.StreamWriteTo" />
internal static class StreamWriteTo
{
	private const long BufferSize = 4096;

	/// <summary>
	/// Writes the input stream to the target file.
	/// </summary>
	/// <param name="source" this="true">The source stream to write to the target file.</param>
	/// <param name="targetFile">The target file to write to.</param>
	/// <param name="append">If set to <see langword="true"/> and the file exists, then appends the source stream, otherwise, it will overwrite it.</param>
	public static void WriteTo(this Stream source, string targetFile, bool append = false)
	{
		using (var output = new FileStream(targetFile, append ? FileMode.Append : FileMode.Create))
		{
			source.WriteTo(output);
		}
	}

	/// <summary>
	/// Writes the input stream to the target stream.
	/// </summary>
	/// <param name="source" this="true">The source stream to write to the target stream.</param>
	/// <param name="target">The target stream to write to.</param>
	/// <returns>The written <paramref name="target"/> stream.</returns>
	public static void WriteTo(this Stream source, Stream target)
	{
		var buffer = new byte[BufferSize];
		var read = 0;
		while ((read = source.Read(buffer, 0, buffer.Length)) != 0)
		{
			target.Write(buffer, 0, read);
		}
	}
}
