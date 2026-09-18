var png = await File.ReadAllBytesAsync(args[0]);
await using var output = File.Create(args[1]);
using var writer = new BinaryWriter(output);
writer.Write((ushort)0); writer.Write((ushort)1); writer.Write((ushort)1);
writer.Write((byte)0); writer.Write((byte)0); writer.Write((byte)0); writer.Write((byte)0);
writer.Write((ushort)1); writer.Write((ushort)32); writer.Write((uint)png.Length); writer.Write((uint)22);
writer.Write(png);
