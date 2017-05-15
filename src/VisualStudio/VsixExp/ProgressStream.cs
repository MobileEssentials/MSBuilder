using System;
using System.IO;

class ProgressStream : Stream
{
    readonly Stream stream;
    readonly IProgress<int> readProgress;
    readonly IProgress<int> writeProgress;

    public ProgressStream(Stream stream, IProgress<int> readProgress, IProgress<int> writeProgress)
    {
        this.stream = stream;
        this.readProgress = readProgress;
        this.writeProgress = writeProgress;
    }

    public override bool CanRead => stream.CanRead;

    public override bool CanSeek => stream.CanSeek;

    public override bool CanWrite => stream.CanWrite;

    public override long Length => stream.Length;

    public override long Position
    {
        get => stream.Position;
        set => stream.Position = value;
    }

    public override void Flush() => stream.Flush();

    public override long Seek(long offset, SeekOrigin origin) => stream.Seek(offset, origin);

    public override void SetLength(long value) => stream.SetLength(value);

    public override int Read(byte[] buffer, int offset, int count)
    {
        int bytesRead = stream.Read(buffer, offset, count);
        readProgress?.Report(bytesRead);
        return bytesRead;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        stream.Write(buffer, offset, count);
        writeProgress?.Report(count);
    }
}