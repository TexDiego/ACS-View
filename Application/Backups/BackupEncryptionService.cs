using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ACS_View.Application.Backups;

public static class BackupEncryptionService
{
    public const int MaxBytes = 64 * 1024 * 1024;
    public const int ChunkBytes = 64 * 1024;
    private const int Iterations = 600_000;
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("ACSVIEW\0");

    public static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length is < 12 or > 128)
            throw new InvalidOperationException("A senha do backup deve ter de 12 a 128 caracteres. Guarde-a: ela não pode ser recuperada.");
    }

    public static async Task EncryptAsync(BackupDocument document, Stream output, string password, CancellationToken ct = default)
    {
        ValidatePassword(password);
        using var payload = new LimitedPayloadStream();
        byte[]? key = null;
        try
        {
            await JsonSerializer.SerializeAsync(payload, document, cancellationToken: ct);
            var jsonLength = checked((int)payload.Length);
            payload.SetLength(jsonLength + 32);
            Buffer.BlockCopy(payload.GetBuffer(), 0, payload.GetBuffer(), 32, jsonLength);
            SHA256.HashData(payload.GetBuffer().AsSpan(32, jsonLength), payload.GetBuffer().AsSpan(0, 32));
            var header = new byte[52];
            Magic.CopyTo(header, 0);
            header[8] = 1; // container version
            header[9] = 1; // AES-256-GCM / PBKDF2-SHA256
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(12), Iterations);
            RandomNumberGenerator.Fill(header.AsSpan(16, 16));
            RandomNumberGenerator.Fill(header.AsSpan(32, 8)); // nonce prefix; counter follows
            BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(40), payload.Length);
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(48), ChunkBytes);
            key = await Task.Run(() => Rfc2898DeriveBytes.Pbkdf2(password, header.AsSpan(16, 16), Iterations, HashAlgorithmName.SHA256, 32), ct);
            using var aes = new AesGcm(key, 16);
            await output.WriteAsync(header, ct);
            var plain = payload.GetBuffer();
            var cipher = new byte[ChunkBytes];
            var tag = new byte[16];
            for (int offset = 0, index = 0; offset < payload.Length; offset += ChunkBytes, index++)
            {
                ct.ThrowIfCancellationRequested();
                int size = (int)Math.Min(ChunkBytes, payload.Length - offset);
                var nonce = Nonce(header, index);
                aes.Encrypt(nonce, plain.AsSpan(offset, size), cipher.AsSpan(0, size), tag, Aad(header, index));
                await output.WriteAsync(cipher.AsMemory(0, size), ct);
                await output.WriteAsync(tag, ct);
            }
            await output.FlushAsync(ct);
        }
        finally { if (key is not null) CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(payload.GetBuffer()); }
    }

    public static async Task<BackupDocument> DecryptAsync(Stream source, string password, CancellationToken ct = default)
    {
        if (password.Length > 128) throw new InvalidDataException("Senha inválida.");
        var header = new byte[52];
        await source.ReadExactlyAsync(header, ct);
        if (!header.AsSpan(0, 8).SequenceEqual(Magic) || header[8] != 1 || header[9] != 1 || header[10] != 0 || header[11] != 0
            || BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(12)) != Iterations
            || BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(48)) != ChunkBytes)
            throw new InvalidDataException("Formato ou versão de backup incompatível.");
        long length = BinaryPrimitives.ReadInt64LittleEndian(header.AsSpan(40));
        if (length is <= 32 or > MaxBytes) throw new InvalidDataException("Tamanho de backup inválido.");
        var key = await Task.Run(() => Rfc2898DeriveBytes.Pbkdf2(password, header.AsSpan(16, 16), Iterations, HashAlgorithmName.SHA256, 32), ct);
        using var payload = new MemoryStream((int)length);
        var plain = new byte[ChunkBytes];
        try
        {
            using var aes = new AesGcm(key, 16);
            var cipher = new byte[ChunkBytes];
            var tag = new byte[16];
            for (int offset = 0, index = 0; offset < length; offset += ChunkBytes, index++)
            {
                int size = (int)Math.Min(ChunkBytes, length - offset);
                await source.ReadExactlyAsync(cipher.AsMemory(0, size), ct);
                await source.ReadExactlyAsync(tag, ct);
                aes.Decrypt(Nonce(header, index), cipher.AsSpan(0, size), tag, plain.AsSpan(0, size), Aad(header, index));
                await payload.WriteAsync(plain.AsMemory(0, size), ct);
            }
            if (await source.ReadAsync(new byte[1], ct) != 0) throw new InvalidDataException("Conteúdo adicional no backup.");
            var digest = SHA256.HashData(payload.GetBuffer().AsSpan(32, (int)payload.Length - 32));
            if (!CryptographicOperations.FixedTimeEquals(digest, payload.GetBuffer().AsSpan(0, 32))) throw new InvalidDataException("Checksum do backup inválido.");
            payload.Position = 32;
            return await JsonSerializer.DeserializeAsync<BackupDocument>(payload, new JsonSerializerOptions { MaxDepth = 32 }, ct)
                ?? throw new InvalidDataException("Backup vazio.");
        }
        catch (CryptographicException) { throw new InvalidDataException("Senha do backup incorreta ou arquivo adulterado."); }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(plain); CryptographicOperations.ZeroMemory(payload.GetBuffer()); }
    }

    private static byte[] Nonce(byte[] header, int index)
    {
        var nonce = new byte[12]; header.AsSpan(32, 8).CopyTo(nonce);
        BinaryPrimitives.WriteInt32BigEndian(nonce.AsSpan(8), index); return nonce;
    }
    private static byte[] Aad(byte[] header, int index)
    {
        var aad = new byte[56]; header.CopyTo(aad, 0);
        BinaryPrimitives.WriteInt32LittleEndian(aad.AsSpan(52), index); return aad;
    }

    private sealed class LimitedPayloadStream : MemoryStream
    {
        private void Check(int count)
        {
            if (Position + count > MaxBytes - 32) throw new InvalidDataException("O backup excede o limite de 64 MiB desta versão.");
        }
        public override void Write(byte[] buffer, int offset, int count) { Check(count); base.Write(buffer, offset, count); }
        public override void Write(ReadOnlySpan<byte> buffer) { Check(buffer.Length); base.Write(buffer); }
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Check(buffer.Length); return base.WriteAsync(buffer, cancellationToken);
        }
    }
}
