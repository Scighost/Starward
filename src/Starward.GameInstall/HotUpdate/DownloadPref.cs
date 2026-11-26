using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Starward.GameInstall.HotUpdate;

internal class DownloadPref
{
    [JsonPropertyName("resCategoryStatusDict")]
    public Dictionary<int, int> ResCategoryStatusDict { get; set; } = new();

    [JsonPropertyName("scriptVersion")]
    public string ScriptVersion { get; set; } = "";

    [JsonPropertyName("incrementVerify")]
    public bool IncrementVerify { get; set; } = true;

    [JsonPropertyName("redownloadUselessAudio")]
    public bool RedownloadUselessAudio { get; set; }

    [JsonPropertyName("redownloadUselessVideo")]
    public bool RedownloadUselessVideo { get; set; }



    public byte[] Encode()
    {
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(this, GenshinHotUpdateJsonContext.Default.DownloadPref);
        byte[] stage1 = Xor(json, K10);
        byte[] b64 = Encoding.ASCII.GetBytes(Convert.ToBase64String(stage1));
        byte[] payload = Xor(b64, K13);

        ushort ck = Checksum(payload, K5);
        var file = new byte[payload.Length + 2];
        file[0] = (byte)(ck & 0xFF);
        file[1] = (byte)(ck >> 8);
        Buffer.BlockCopy(payload, 0, file, 2, payload.Length);
        return file;
    }



    public static DownloadPref Decode(byte[] file)
    {
        if (file is null || file.Length < 2)
        {
            throw new InvalidDataException("invalid readBytes");
        }

        int n = file.Length - 2;
        var payload = new byte[n];
        Buffer.BlockCopy(file, 2, payload, 0, n);

        ushort stored = (ushort)(file[0] | (file[1] << 8));
        if (Checksum(payload, K5) != stored)
            throw new InvalidDataException("DownloadPref decode pref content error, checksum not correct");

        byte[] b64 = Xor(payload, K13);
        byte[] stage1 = Convert.FromBase64String(Encoding.ASCII.GetString(b64));
        byte[] json = Xor(stage1, K10);
        return JsonSerializer.Deserialize(json, GenshinHotUpdateJsonContext.Default.DownloadPref)!;
    }


    static byte[] Xor(byte[] data, byte[] key)
    {
        var res = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            res[i] = (byte)(data[i] ^ key[i % key.Length]);
        }
        return res;
    }


    static ushort Checksum(byte[] data, byte[] key)
    {
        int sum = 0;
        for (int i = 0; i < data.Length; i++)
        {
            sum = (sum + ((key[i % key.Length] * data[i]) & 0xFF)) & 0xFFFF;
        }
        return (ushort)sum;
    }


    static readonly byte[] K13 = { 0xAC, 0xAA, 0x13, 0x08, 0x28, 0x12, 0x69, 0xF2, 0xC6, 0x26, 0x99, 0x87, 0x82 };
    static readonly byte[] K10 = { 0xEA, 0xAB, 0x1C, 0x32, 0x12, 0x88, 0x77, 0x61, 0x58, 0x38 };
    static readonly byte[] K5 = { 0xEA, 0x32, 0x51, 0x69, 0xC8 };

}