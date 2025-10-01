
namespace Library.Helpers;

public static class EnumTools
{
    public static string EnumValuesToString(Type type, 
        char separator = ';')
    {
        var output = "";
        foreach (var value in Enum.GetNames(type))
        {
            if (output != "")
                output += separator;

            output += value.ToString();
        }

        output += separator;

        return output;
    }
}