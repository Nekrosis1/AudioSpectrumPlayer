using AudioSpectrumPlayer.Interfaces;
using AudioSpectrumPlayer.Models;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace AudioSpectrumPlayer.Services
{
	/// <summary>
	/// Decodes uncompressed RIFF/WAVE files: integer PCM (8/16/24/32-bit) and IEEE float
	/// (32/64-bit), any channel count, plain and WAVE_FORMAT_EXTENSIBLE headers.
	/// </summary>
	/// <remarks>
	/// Valid but unsupported files (compressed audio, other bit depths) throw
	/// <see cref="NotSupportedException"/>; broken or non-WAV input throws
	/// <see cref="InvalidDataException"/>.
	/// </remarks>
	public class WavDecoder : IPcmDecoder
	{
		private const ushort FormatPcm = 0x0001;
		private const ushort FormatIeeeFloat = 0x0003;
		private const ushort FormatExtensible = 0xFFFE;

		private const int MinFmtSize = 16;
		private const int ExtensibleFmtSize = 40;
		private const int SubFormatOffset = 24;

		public PcmData Decode(string filePath)
		{
			using FileStream stream = File.OpenRead(filePath);
			return Decode(stream);
		}

		public PcmData Decode(Stream stream)
		{
			ArgumentNullException.ThrowIfNull(stream);

			ReadRiffHeader(stream);

			WavFormat? format = null;
			byte[]? data = null;

			while (format is null || data is null)
			{
				if (!TryReadChunkHeader(stream, out string id, out uint size))
				{
					string missing = format is null && data is null ? "'fmt ' and 'data' chunks"
						: format is null ? "'fmt ' chunk"
						: "'data' chunk";
					throw new InvalidDataException($"Invalid WAV: no {missing} found before the end of the file.");
				}

				switch (id)
				{
					case "fmt " when format is null:
						format = ParseFormat(ReadChunkBody(stream, id, size));
						break;
					case "data" when data is null:
						data = ReadChunkBody(stream, id, size);
						break;
					default:
						Skip(stream, id, size);
						break;
				}

				if ((size & 1) != 0 && (format is null || data is null))
				{
					// RIFF pads odd-sized chunks to an even length. A missing pad byte at the very
					// end is tolerated; the next header read then reports what is missing.
					SkipPadByte(stream);
				}
			}

			return ToPcmData(format, data);
		}

		private static void ReadRiffHeader(Stream stream)
		{
			Span<byte> header = stackalloc byte[12];
			int read = stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);
			if (read < header.Length)
			{
				throw new InvalidDataException($"Invalid WAV: file is only {read} bytes long, too short for a RIFF header.");
			}

			string riff = Encoding.ASCII.GetString(header[..4]);
			string wave = Encoding.ASCII.GetString(header[8..12]);
			if (riff != "RIFF" || wave != "WAVE")
			{
				throw new InvalidDataException(
					$"Invalid WAV: expected 'RIFF'/'WAVE' header, found '{Printable(riff)}'/'{Printable(wave)}'.");
			}
		}

		/// <returns>False at a clean end of the stream (no bytes left).</returns>
		private static bool TryReadChunkHeader(Stream stream, out string id, out uint size)
		{
			Span<byte> header = stackalloc byte[8];
			int read = stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);
			if (read == 0)
			{
				id = string.Empty;
				size = 0;
				return false;
			}
			if (read < header.Length)
			{
				throw new InvalidDataException($"Invalid WAV: truncated chunk header ({read} of 8 bytes).");
			}

			id = Encoding.ASCII.GetString(header[..4]);
			size = BinaryPrimitives.ReadUInt32LittleEndian(header[4..]);
			return true;
		}

		private static byte[] ReadChunkBody(Stream stream, string id, uint size)
		{
			// The size comes from the file: check it against what is really there before
			// allocating, so a corrupt header can't force a huge allocation.
			if (stream.CanSeek)
			{
				ThrowIfTruncated(stream, id, size);
			}
			if (size > Array.MaxLength)
			{
				throw new NotSupportedException(
					$"Unsupported WAV: '{Printable(id)}' chunk of {size} bytes is larger than the {Array.MaxLength} bytes that can be loaded.");
			}

			if (stream.CanSeek)
			{
				byte[] body = new byte[size];
				stream.ReadExactly(body);
				return body;
			}

			// Unknown length: read in pieces so memory only grows with the bytes actually present.
			using MemoryStream collected = new();
			byte[] buffer = new byte[Math.Min(size, 81920u)];
			long left = size;
			while (left > 0)
			{
				int read = stream.Read(buffer, 0, (int)Math.Min(left, buffer.Length));
				if (read == 0)
				{
					throw new InvalidDataException(
						$"Invalid WAV: truncated '{Printable(id)}' chunk, header says {size} bytes but only {size - left} are present.");
				}
				collected.Write(buffer, 0, read);
				left -= read;
			}
			return collected.ToArray();
		}

		private static void ThrowIfTruncated(Stream stream, string id, uint size)
		{
			long remaining = stream.Length - stream.Position;
			if (size > remaining)
			{
				throw new InvalidDataException(
					$"Invalid WAV: truncated '{Printable(id)}' chunk, header says {size} bytes but only {remaining} are present.");
			}
		}

		private static void Skip(Stream stream, string id, uint size)
		{
			if (stream.CanSeek)
			{
				ThrowIfTruncated(stream, id, size);
				stream.Seek(size, SeekOrigin.Current);
				return;
			}

			byte[] buffer = new byte[Math.Min(size, 81920u)];
			long left = size;
			while (left > 0)
			{
				int read = stream.Read(buffer, 0, (int)Math.Min(left, buffer.Length));
				if (read == 0)
				{
					throw new InvalidDataException(
						$"Invalid WAV: truncated '{Printable(id)}' chunk, header says {size} bytes but only {size - left} are present.");
				}
				left -= read;
			}
		}

		private static void SkipPadByte(Stream stream)
		{
			Span<byte> pad = stackalloc byte[1];
			_ = stream.Read(pad);
		}

		private static WavFormat ParseFormat(byte[] fmt)
		{
			if (fmt.Length < MinFmtSize)
			{
				throw new InvalidDataException($"Invalid WAV: 'fmt ' chunk is {fmt.Length} bytes, at least {MinFmtSize} expected.");
			}

			ushort formatTag = BinaryPrimitives.ReadUInt16LittleEndian(fmt.AsSpan(0));
			ushort channels = BinaryPrimitives.ReadUInt16LittleEndian(fmt.AsSpan(2));
			uint sampleRate = BinaryPrimitives.ReadUInt32LittleEndian(fmt.AsSpan(4));
			ushort blockAlign = BinaryPrimitives.ReadUInt16LittleEndian(fmt.AsSpan(12));
			ushort bitsPerSample = BinaryPrimitives.ReadUInt16LittleEndian(fmt.AsSpan(14));

			ushort sampleFormat = formatTag;
			if (formatTag == FormatExtensible)
			{
				if (fmt.Length < ExtensibleFmtSize)
				{
					throw new InvalidDataException(
						$"Invalid WAV: EXTENSIBLE 'fmt ' chunk is {fmt.Length} bytes, at least {ExtensibleFmtSize} expected.");
				}
				// The first 2 bytes of the SubFormat GUID hold the actual format tag.
				sampleFormat = BinaryPrimitives.ReadUInt16LittleEndian(fmt.AsSpan(SubFormatOffset));
			}

			string formatName = DescribeFormat(formatTag, sampleFormat);
			bool supported = sampleFormat switch
			{
				FormatPcm => bitsPerSample is 8 or 16 or 24 or 32,
				FormatIeeeFloat => bitsPerSample is 32 or 64,
				_ => throw new NotSupportedException($"Unsupported WAV: {formatName} (compressed or unknown format)."),
			};
			if (!supported)
			{
				throw new NotSupportedException($"Unsupported WAV: {formatName} with {bitsPerSample} bits per sample.");
			}

			if (channels == 0)
			{
				throw new InvalidDataException("Invalid WAV: 'fmt ' chunk says 0 channels.");
			}
			if (sampleRate == 0 || sampleRate > int.MaxValue)
			{
				throw new InvalidDataException($"Invalid WAV: 'fmt ' chunk says sample rate {sampleRate}.");
			}

			int bytesPerSample = bitsPerSample / 8;
			int frameSize = channels * bytesPerSample;
			if (blockAlign != frameSize)
			{
				throw new InvalidDataException(
					$"Invalid WAV: block align is {blockAlign}, but {channels} channels × {bitsPerSample} bits need {frameSize}.");
			}

			return new WavFormat(sampleFormat, bytesPerSample, channels, (int)sampleRate, frameSize);
		}

		private static string DescribeFormat(ushort formatTag, ushort sampleFormat)
		{
			string sampleFormatText = sampleFormat switch
			{
				FormatPcm => $"format tag 0x{sampleFormat:X4} (PCM)",
				FormatIeeeFloat => $"format tag 0x{sampleFormat:X4} (IEEE float)",
				_ => $"format tag 0x{sampleFormat:X4}",
			};
			return formatTag == FormatExtensible ? $"EXTENSIBLE (0xFFFE) with sub-{sampleFormatText}" : sampleFormatText;
		}

		private static PcmData ToPcmData(WavFormat format, byte[] data)
		{
			if (data.Length % format.FrameSize != 0)
			{
				throw new InvalidDataException(
					$"Invalid WAV: 'data' chunk is {data.Length} bytes, not a whole number of {format.FrameSize}-byte frames.");
			}

			int sampleCount = data.Length / format.BytesPerSample;
			float[] samples = new float[sampleCount];
			ReadOnlySpan<byte> bytes = data;

			switch (format.SampleFormat, format.BytesPerSample)
			{
				case (FormatPcm, 1):
					for (int i = 0; i < sampleCount; i++)
					{
						// 8-bit WAV is unsigned, 128 is silence.
						samples[i] = (bytes[i] - 128) / 128f;
					}
					break;
				case (FormatPcm, 2):
					for (int i = 0; i < sampleCount; i++)
					{
						samples[i] = BinaryPrimitives.ReadInt16LittleEndian(bytes.Slice(i * 2)) / 32768f;
					}
					break;
				case (FormatPcm, 3):
					for (int i = 0; i < sampleCount; i++)
					{
						int o = i * 3;
						// Casting the top byte through sbyte sign-extends the 24-bit value.
						int value = bytes[o] | (bytes[o + 1] << 8) | ((sbyte)bytes[o + 2] << 16);
						samples[i] = value / 8388608f;
					}
					break;
				case (FormatPcm, 4):
					for (int i = 0; i < sampleCount; i++)
					{
						samples[i] = BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(i * 4)) / 2147483648f;
					}
					break;
				case (FormatIeeeFloat, 4):
					for (int i = 0; i < sampleCount; i++)
					{
						samples[i] = BinaryPrimitives.ReadSingleLittleEndian(bytes.Slice(i * 4));
					}
					break;
				case (FormatIeeeFloat, 8):
					for (int i = 0; i < sampleCount; i++)
					{
						samples[i] = (float)BinaryPrimitives.ReadDoubleLittleEndian(bytes.Slice(i * 8));
					}
					break;
				default:
					// ParseFormat only lets the combinations above through.
					throw new InvalidOperationException($"Unhandled sample format {format.SampleFormat} with {format.BytesPerSample} bytes.");
			}

			return new PcmData(samples, format.SampleRate, format.Channels);
		}

		/// <summary>Chunk ids and magic numbers from a non-WAV file can contain control characters.</summary>
		private static string Printable(string text)
		{
			StringBuilder builder = new(text.Length);
			foreach (char c in text)
			{
				builder.Append(c is >= ' ' and <= '~' ? c : '?');
			}
			return builder.ToString();
		}

		private record WavFormat(ushort SampleFormat, int BytesPerSample, int Channels, int SampleRate, int FrameSize);
	}
}
