using System;
using System.IO;
using System.Text;
using static Nito.HashAlgorithms.CRC32;
namespace Pandora.Patch.Patchers.Skyrim.AnimSetData;

public static class BSCRC32
{
    public static Definition BSDefinition = new()
    {
        Initializer = 0,
        TruncatedPolynomial = 0x04C11DB7,
        FinalXorValue = 0,
        ReverseResultBeforeFinalXor = true,
        ReverseDataBytes = true
    };

    public static byte[] GetValue(byte[] bytes)
    {

        using Nito.HashAlgorithms.CRC32 alg = new(BSDefinition);
        byte[] crc32 = alg.ComputeHash(bytes);
        return crc32;
    }

    public static uint GetValueUInt32(string str)
    {
        return BitConverter.ToUInt32(GetValue(Encoding.ASCII.GetBytes(str)));
    }

    public static string GetValueString(string str)
    {
        return GetValueUInt32(str).ToString();
    }
}
public class SetCachedAnimInfo
{

    public uint encodedPath { get; private set; } = 3064642194; //vanilla actor animation folder path

    public uint encodedFileName { get; private set; } = 0; //animation name in lowercase

    public uint encodedExtension { get; private set; } = 7891816; //xkh

    public static SetCachedAnimInfo Read(StreamReader reader)
    {
        SetCachedAnimInfo animInfo = new()
        {
            encodedPath = uint.Parse(reader.ReadLineSafe()),
            encodedFileName = uint.Parse(reader.ReadLineSafe()),
            encodedExtension = uint.Parse(reader.ReadLineSafe())
        };

        return animInfo;
    }

    public static SetCachedAnimInfo Encode(string folderPath, string fileName) //filename without extension
    {
        SetCachedAnimInfo animInfo = new()
        {
            encodedPath = BSCRC32.GetValueUInt32(folderPath.ToLower()),
            encodedFileName = BSCRC32.GetValueUInt32(fileName.ToLower())
        };

        return animInfo;
    }

    public override string ToString()
    {
        StringBuilder sb = new();

        _ = sb.AppendLine(this.encodedPath.ToString());
        _ = sb.AppendLine(this.encodedFileName.ToString());
        _ = sb.AppendLine(this.encodedExtension.ToString());

        return sb.ToString();
    }

}
