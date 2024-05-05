using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Pandora.Patch.Patchers.Skyrim.AnimData;

public class ClipDataBlock
{
    public string Name { get; private set; } = string.Empty;

    public string ClipID { get; private set; } = string.Empty;

    public float PlaybackSpeed { get; private set; } = 1.0f;
    public float CropStartLocalTime { get; private set; } = 0.0f;
    public float CropEndLocalTime { get; private set; } = 0.0f;

    public int NumClipTriggers { get; private set; } = 0;

    public List<string> TriggerNames { get; private set; } = new List<string>();

    public ClipDataBlock()
    {

    }

    public ClipDataBlock(string name, string id)
    {
        this.Name = name;
        this.ClipID = id;
    }
    private static string ReadLineSafe(StreamReader reader)
    {
        string? expectedLine = reader.ReadLine();
        return expectedLine ?? string.Empty;
    }
    public static ClipDataBlock ReadBlock(StreamReader reader)
    {
        ClipDataBlock block = new();
        try
        {
            block.Name = ReadLineSafe(reader);

            block.ClipID = ReadLineSafe(reader);
            block.PlaybackSpeed = float.Parse(ReadLineSafe(reader));
            block.CropStartLocalTime = float.Parse(ReadLineSafe(reader));
            block.CropEndLocalTime = float.Parse(ReadLineSafe(reader));

            block.NumClipTriggers = int.Parse(ReadLineSafe(reader));

            for (int i = 0; i < block.NumClipTriggers; i++)
            {
                block.TriggerNames.Add(ReadLineSafe(reader));
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message + " in ", ex);
        }
        return block;

    }

    public static ClipDataBlock LoadBlock(string filePath)
    {
        using StreamReader reader = new(filePath);
        return ReadBlock(reader);
    }

    public override string ToString()
    {

        StringBuilder sb = new();

        _ = sb.AppendLine(this.Name).AppendLine(this.ClipID).AppendLine(this.PlaybackSpeed.ToString()).AppendLine(this.CropStartLocalTime.ToString()).AppendLine(this.CropEndLocalTime.ToString()).AppendLine(this.NumClipTriggers.ToString());

        if (this.TriggerNames.Count > 0)
        {

            _ = sb.AppendJoin("\r\n", this.TriggerNames).AppendLine();

        }
        return sb.ToString();
    }

    public int GetLineCount()
    {
        return 6 + this.TriggerNames.Count;
    }
}
