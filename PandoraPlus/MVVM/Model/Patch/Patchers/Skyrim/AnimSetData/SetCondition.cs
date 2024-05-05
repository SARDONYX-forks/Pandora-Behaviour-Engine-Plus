using System.IO;
using System.Text;

namespace Pandora.Patch.Patchers.Skyrim.AnimSetData;

public class SetCondition
{
    public string VariableName { get; private set; } = string.Empty;

    public int Value1 { get; private set; } = 0;

    public int Value2 { get; private set; } = 0;

    public static SetCondition ReadCondition(StreamReader reader)
    {
        SetCondition condition = new()
        {
            VariableName = reader.ReadLineSafe()
        };

        if (!int.TryParse(reader.ReadLineSafe(), out int value1) || !int.TryParse(reader.ReadLineSafe(), out int value2))
        {
            return condition;
        }

        condition.Value1 = value1;
        condition.Value2 = value2;

        return condition;
    }

    public override string ToString()
    {
        StringBuilder sb = new();

        _ = sb.AppendLine(this.VariableName);
        _ = sb.AppendLine(this.Value1.ToString());
        _ = sb.AppendLine(this.Value2.ToString());

        return sb.ToString();

    }
}
