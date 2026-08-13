using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Borderlands4SaveDataResignerCore.Deencryption.Aes256;

/// <summary>
/// Provides AES-256-ECB encryption and decryption operations accelerated by AES-NI hardware intrinsics.
/// </summary>
/// <remarks>
/// This static class leverages SSE2 and AES hardware instructions to perform 128-bit block-level encryption
/// and decryption at native hardware speed.
/// Before calling <see cref="Encrypt" /> or <see cref="Decrypt" />, the 256-bit key must be expanded into a round key
/// buffer of <see cref="RoundKeyBufferLength" /> bytes using <see cref="ExpandKey256" />.
/// For decryption, the expanded round key schedule must additionally be inverted via <see cref="InvertKeySchedule" />.
/// Call <see cref="IsAesNiSupported" /> to verify that the current runtime environment provides the required
/// AES and SSE2 instruction set extensions before using this class.
/// All input and output buffers must be aligned to 16-byte boundaries and have a length that is a multiple of 16.
/// </remarks>
public static class AesEcb256Ni
{
    /// <summary>
    /// The length in bytes of the expanded AES-256 round key buffer, equal to 15 round keys multiplied by 16 bytes each (240 bytes total).
    /// </summary>
    public const int RoundKeyBufferLength = 240;

    /// <summary>
    /// Determines whether the current processor supports AES-NI hardware instructions and SSE2 extensions
    /// required for the accelerated AES-256 ECB backend.
    /// </summary>
    /// <return><see langword="true"/> if both AES and SSE2 instruction sets are available on the current platform; otherwise <see langword="false"/>.</return>
    public static bool IsAesNiSupported() =>
        Aes.IsSupported && Sse2.IsSupported;

    /// <summary>
    /// Encrypts the specified plaintext data using AES-256 in Electronic Code Book (ECB) mode,
    /// accelerated by AES-NI and SSE2 hardware instructions.
    /// </summary>
    /// <param name="roundKeyBuffer">The expanded 256-bit round key schedule, produced by calling <see cref="ExpandKey256"/>. The buffer must contain <see cref="RoundKeyBufferLength"/> bytes and be aligned to a 16-byte boundary.</param>
    /// <param name="plaintext">The input data to encrypt. The span must be aligned to a 16-byte boundary and its length must be a positive multiple of 16 bytes.</param>
    /// <param name="ciphertext">The output span that receives the encrypted blocks. The span must be aligned to a 16-byte boundary and must have a length equal to the length of <paramref name="plaintext"/>.</param>
    public static void Encrypt(ReadOnlySpan<byte> roundKeyBuffer, ReadOnlySpan<byte> plaintext, Span<byte> ciphertext)
    {
        var roundKeys = MemoryMarshal.Cast<byte, Vector128<byte>>(roundKeyBuffer);
        var inBlocks = MemoryMarshal.Cast<byte, Vector128<byte>>(plaintext);
        var outBlocks = MemoryMarshal.Cast<byte, Vector128<byte>>(ciphertext);

        for (var i = 0; i < inBlocks.Length; i++)
        {
            var state = inBlocks[i];
           
            state = Sse2.Xor(state, roundKeys[0]);
            state = Aes.Encrypt(state, roundKeys[1]);
            state = Aes.Encrypt(state, roundKeys[2]);
            state = Aes.Encrypt(state, roundKeys[3]);
            state = Aes.Encrypt(state, roundKeys[4]);
            state = Aes.Encrypt(state, roundKeys[5]);
            state = Aes.Encrypt(state, roundKeys[6]);
            state = Aes.Encrypt(state, roundKeys[7]);
            state = Aes.Encrypt(state, roundKeys[8]);
            state = Aes.Encrypt(state, roundKeys[9]);
            state = Aes.Encrypt(state, roundKeys[10]);
            state = Aes.Encrypt(state, roundKeys[11]);
            state = Aes.Encrypt(state, roundKeys[12]);
            state = Aes.Encrypt(state, roundKeys[13]);
            state = Aes.EncryptLast(state, roundKeys[14]);
           
            outBlocks[i] = state;
        }
    }

    /// <summary>
    /// Decrypts one or more 128-bit AES-ECB blocks from the given ciphertext span into the plaintext
    /// span using the supplied inverted round key schedule and AES-NI hardware intrinsics.
    /// </summary>
    /// <param name="inversedRoundKeyBuffer">The inverted 256-bit round key schedule (160 bytes), previously produced by
    /// <see cref="ExpandKey256" /> and <see cref="InvertKeySchedule" />, interpreted as 10 SSE2 vector registers.
    /// </param>
    /// <param name="ciphertext">The ciphertext input to be decrypted. The span length must be a multiple of 16 bytes (i.e. a whole number of 128-bit AES blocks) and must be 16-byte aligned.
    /// </param>
    /// <param name="plaintext">The destination span that receives the decrypted plaintext.
    /// The span must be at least as long as <paramref name="ciphertext" />, 16-byte aligned, and may overlap with the ciphertext only if it does not violate block boundaries.
    /// </param>
    public static void Decrypt(ReadOnlySpan<byte> inversedRoundKeyBuffer, ReadOnlySpan<byte> ciphertext,
        Span<byte> plaintext)
    {
        var roundKeys = MemoryMarshal.Cast<byte, Vector128<byte>>(inversedRoundKeyBuffer);
        var inBlocks = MemoryMarshal.Cast<byte, Vector128<byte>>(ciphertext);
        var outBlocks = MemoryMarshal.Cast<byte, Vector128<byte>>(plaintext);
        
        for (var i = 0; i < inBlocks.Length; i++)
        {
            var state = inBlocks[i];
           
            state = Sse2.Xor(state, roundKeys[0]);
            state = Aes.Decrypt(state, roundKeys[1]);
            state = Aes.Decrypt(state, roundKeys[2]);
            state = Aes.Decrypt(state, roundKeys[3]);
            state = Aes.Decrypt(state, roundKeys[4]);
            state = Aes.Decrypt(state, roundKeys[5]);
            state = Aes.Decrypt(state, roundKeys[6]);
            state = Aes.Decrypt(state, roundKeys[7]);
            state = Aes.Decrypt(state, roundKeys[8]);
            state = Aes.Decrypt(state, roundKeys[9]);
            state = Aes.Decrypt(state, roundKeys[10]);
            state = Aes.Decrypt(state, roundKeys[11]);
            state = Aes.Decrypt(state, roundKeys[12]);
            state = Aes.Decrypt(state, roundKeys[13]);
            state = Aes.DecryptLast(state, roundKeys[14]);

            outBlocks[i] = state;
        }
    }

    /// <summary>
    /// Computes the AES-256 key schedule round constant (RCon) for the specified round by
    /// applying the AES KeygenAssist instruction with the appropriate RCon value.
    /// </summary>
    /// <param name="temp">The 128-bit input vector representing the high portion of the round key state to be transformed.</param>
    /// <param name="round">The zero-based round index, valid values are 0 through 6, each corresponding to a distinct RCon value (0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40).</param>
    /// <return>A <see cref="Vector128{Byte}"/> containing the result of applying the AES KeygenAssist operation with the RCon value for the given round.</return>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="round"/> is less than 0 or greater than 6.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<byte> KeygenAssistRcon(Vector128<byte> temp, int round)
    {
        return round switch
        {
            0 => Aes.KeygenAssist(temp, 0x01),
            1 => Aes.KeygenAssist(temp, 0x02),
            2 => Aes.KeygenAssist(temp, 0x04),
            3 => Aes.KeygenAssist(temp, 0x08),
            4 => Aes.KeygenAssist(temp, 0x10),
            5 => Aes.KeygenAssist(temp, 0x20),
            6 => Aes.KeygenAssist(temp, 0x40),
            _ => throw new ArgumentOutOfRangeException(nameof(round))
        };
    }

    /// <summary>
    /// Inverts the AES-256 round key schedule in place by reversing the round key order and applying the InvMixColumns
    /// transformation to intermediate rounds, producing the key buffer required for hardware-accelerated decryption.
    /// </summary>
    /// <param name="roundKeys">The expanded 256-bit round key buffer (240 bytes) produced by <see cref="ExpandKey256"/>, to be read for the inversion process.</param>
    /// <param name="invertedRoundKeys">The target buffer (240 bytes) that receives the inverted round key schedule suitable for use with the decryption path.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvertKeySchedule(this ReadOnlySpan<byte> roundKeys, Span<byte> invertedRoundKeys)
    {
        var key = MemoryMarshal.Read<Vector128<byte>>(roundKeys[224..]);
        MemoryMarshal.Write(invertedRoundKeys, in key);
        
        for (var round = 13; round >= 1; --round)
        {
            key = MemoryMarshal.Read<Vector128<byte>>(roundKeys[(round * 16)..]);
            key = Aes.InverseMixColumns(key);
            var dstOffset = (14 - round) * 16;
            MemoryMarshal.Write(invertedRoundKeys[dstOffset..], in key);
        }
        
        key = MemoryMarshal.Read<Vector128<byte>>(roundKeys);
        MemoryMarshal.Write(invertedRoundKeys[224..], in key);
    }

    /// <summary>
    /// Expands a 256-bit AES key into the full set of round keys for AES-256 ECB decryption,
    /// writing the result into the provided round key buffer. The key is read as two
    /// 128-bit vector halves and iteratively expanded through seven AES-256 key schedule
    /// rounds using the <see cref="ExpandRound"/> routine.
    /// </summary>
    /// <param name="key256">A read-only span containing the 256-bit (32-byte) AES master key to be expanded.</param>
    /// <param name="roundKeyBuffer">A writable span of at least 256 bytes that receives the fully expanded round key material, including the initial 256-bit key block followed by the derived round keys.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ExpandKey256(this ReadOnlySpan<byte> key256, Span<byte> roundKeyBuffer)
    {
        var lo = MemoryMarshal.Read<Vector128<byte>>(key256);
        var hi = MemoryMarshal.Read<Vector128<byte>>(key256[16..]);

        MemoryMarshal.Write(roundKeyBuffer, in lo);
        MemoryMarshal.Write(roundKeyBuffer[16..], in hi);
        ExpandRound(ref lo, ref hi, roundKeyBuffer[32..], 0);
        ExpandRound(ref lo, ref hi, roundKeyBuffer[64..], 1);
        ExpandRound(ref lo, ref hi, roundKeyBuffer[96..], 2);
        ExpandRound(ref lo, ref hi, roundKeyBuffer[128..], 3);
        ExpandRound(ref lo, ref hi, roundKeyBuffer[160..], 4);
        ExpandRound(ref lo, ref hi, roundKeyBuffer[192..], 5);
        ExpandRound(ref lo, ref hi, roundKeyBuffer[224..], 6, true);
    }

    /// <summary>
    /// Performs a single round of the AES-256 key schedule expansion, computing the next round key pair from the current
    /// <c>lo</c> and <c>hi</c> 128-bit vector halves and writing the resulting 32 bytes into the destination buffer.
    /// </summary>
    /// <param name="lo">The lower 128-bit half of the current round key material, passed by reference and updated in place with the newly computed lower half.</param>
    /// <param name="hi">The upper 128-bit half of the current round key material, passed by reference and updated in place with the newly computed upper half. When <paramref name="isFinal"/> is <see langword="true"/>, this value is not modified.</param>
    /// <param name="destination">A writable span of at least 32 bytes that receives the expanded round key bytes for this round. The lower half is written at offset 0 and the upper half at offset 16.</param>
    /// <param name="round">The zero-based round index used to select the appropriate round constant (Rcon) during key generation assist computation.</param>
    /// <param name="isFinal">When <see langword="true"/>, only the lower half expansion is performed and the method returns early, skipping the upper half computation and write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ExpandRound(ref Vector128<byte> lo, ref Vector128<byte> hi, Span<byte> destination, int round,
        bool isFinal = false)
    {
        var assist = KeygenAssistRcon(hi, round);
        assist = Sse2.Shuffle(assist.AsUInt32(), 0xFF).AsByte();
        
        var t = lo;
        t = Sse2.Xor(t, Sse2.ShiftLeftLogical128BitLane(t, 4));
        t = Sse2.Xor(t, Sse2.ShiftLeftLogical128BitLane(t, 8));

        lo = Sse2.Xor(t, assist);
        MemoryMarshal.Write(destination, in lo);

        if (isFinal) return;
        
        assist = Aes.KeygenAssist(lo, 0x00);
        assist = Sse2.Shuffle(assist.AsUInt32(), 0xAA).AsByte();

        t = hi;
        t = Sse2.Xor(t, Sse2.ShiftLeftLogical128BitLane(t, 4));
        t = Sse2.Xor(t, Sse2.ShiftLeftLogical128BitLane(t, 8));

        hi = Sse2.Xor(t, assist);
        MemoryMarshal.Write(destination[16..], in hi);
    }
}