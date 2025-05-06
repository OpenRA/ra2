#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenRA.FileSystem;
using OpenRA.Mods.Cnc.FileFormats;
using OpenRA.Primitives;
using FS = OpenRA.FileSystem.FileSystem;

namespace OpenRA.Mods.Cnc.FileSystem
{
	public sealed class AudioBagLoader : IPackageLoader
	{
		sealed class BagFile : IReadOnlyPackage
		{
			public string Name { get; }

			public IEnumerable<string> Contents => index.Keys;

			readonly Stream s;
			readonly Dictionary<string, IdxEntry> index;

			public BagFile(Stream s, List<IdxEntry> entries, string filename)
			{
				Name = filename;
				this.s = s;

				index = entries.ToDictionaryWithConflictLog(x => x.Filename, $"{filename} (bag format)", null, x => $"(offs={x.Offset}, len={x.Length})");
			}

			public Stream GetStream(string filename)
			{
				if (!index.TryGetValue(filename, out var entry))
					return null;

				var waveHeaderMemoryStream = new MemoryStream();

				var channels = (entry.Flags & 1) > 0 ? 2 : 1;

				if ((entry.Flags & 2) > 0)
				{
					// PCM
					waveHeaderMemoryStream.Write(Encoding.ASCII.GetBytes("RIFF"));
					waveHeaderMemoryStream.Write(BitConverter.GetBytes(entry.Length + 36));
					waveHeaderMemoryStream.Write(Encoding.ASCII.GetBytes("WAVE"));
					waveHeaderMemoryStream.Write(Encoding.ASCII.GetBytes("fmt "));
					waveHeaderMemoryStream.Write(BitConverter.GetBytes(16));
					waveHeaderMemoryStream.Write(BitConverter.GetBytes((short)1));
					waveHeaderMemoryStream.Write(BitConverter.GetBytes((short)channels));
					waveHeaderMemoryStream.Write(BitConverter.GetBytes(entry.SampleRate));
					waveHeaderMemoryStream.Write(BitConverter.GetBytes(2 * channels * entry.SampleRate));
					waveHeaderMemoryStream.Write(BitConverter.GetBytes((short)(2 * channels)));
					waveHeaderMemoryStream.Write(BitConverter.GetBytes((short)16));
					waveHeaderMemoryStream.Write(Encoding.ASCII.GetBytes("data"));
					waveHeaderMemoryStream.Write(BitConverter.GetBytes(entry.Length));
				}

				if ((entry.Flags & 8) > 0)
				{
					// IMA ADPCM
					var samplesPerChunk = 2 * (entry.ChunkSize - 4) + 1;
					var bytesPerSec = (int)Math.Floor((double)(2 * entry.ChunkSize) / samplesPerChunk * ((double)entry.SampleRate / 2));
					var chunkSize = entry.ChunkSize > entry.Length ? entry.Length : entry.ChunkSize;

					waveHeaderMemoryStream.Write(Encoding.ASCII.GetBytes("RIFF"));
					waveHeaderMemoryStream.Write(BitConverter.GetBytes(entry.Length + 52));
					waveHeaderMemoryStream.Write(Encoding.ASCII.GetBytes("WAVE"));
					waveHeaderMemoryStream.Write(Encoding.ASCII.GetBytes("fmt "));
					waveHeaderMemoryStream.Write(BitConverter.GetBytes(20));
					waveHeaderMemoryStream.Write(BitConverter.GetBytes((short)17));
					waveHeaderMemoryStream.Write(BitConverter.GetBytes((short)channels));
					waveHeaderMemoryStream.Write(BitConverter.GetBytes(entry.SampleRate));
					waveHeaderMemoryStream.Write(BitConverter.GetBytes(bytesPerSec));
					waveHeaderMemoryStream.Write(BitConverter.GetBytes((short)chunkSize));
					waveHeaderMemoryStream.Write(BitConverter.GetBytes((short)4));
					waveHeaderMemoryStream.Write(BitConverter.GetBytes((short)2));
					waveHeaderMemoryStream.Write(BitConverter.GetBytes((short)samplesPerChunk));
					waveHeaderMemoryStream.Write(Encoding.ASCII.GetBytes("fact"));
					waveHeaderMemoryStream.Write(BitConverter.GetBytes(4));
					waveHeaderMemoryStream.Write(BitConverter.GetBytes(4 * entry.Length));
					waveHeaderMemoryStream.Write(Encoding.ASCII.GetBytes("data"));
					waveHeaderMemoryStream.Write(BitConverter.GetBytes(entry.Length));
				}

				waveHeaderMemoryStream.Seek(0, SeekOrigin.Begin);

				// Construct a merged stream
				var waveStream = SegmentStream.CreateWithoutOwningStream(s, entry.Offset, (int)entry.Length);
				return new MergedStream(waveHeaderMemoryStream, waveStream);
			}

			public bool Contains(string filename)
			{
				return index.ContainsKey(filename);
			}

			public IReadOnlyPackage OpenPackage(string filename, FS context)
			{
				// Not implemented
				return null;
			}

			public void Dispose()
			{
				s.Dispose();
			}
		}

		bool IPackageLoader.TryParsePackage(Stream s, string filename, FS context, out IReadOnlyPackage package)
		{
			if (!filename.EndsWith(".bag", StringComparison.InvariantCultureIgnoreCase))
			{
				package = null;
				return false;
			}

			// A bag file is always accompanied with an .idx counterpart
			// For example: audio.bag requires the audio.idx file
			var indexFilename = Path.ChangeExtension(filename, ".idx");
			List<IdxEntry> entries = null;

			try
			{
				// Build the index and dispose the stream, it is no longer needed after this
				using (var indexStream = context.Open(indexFilename))
					entries = new IdxReader(indexStream).Entries;
			}
			catch
			{
				package = null;
				return false;
			}

			package = new BagFile(s, entries, filename);
			return true;
		}
	}
}
