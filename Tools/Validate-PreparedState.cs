using System;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using ForestShop.Core;
public static class ValidatePreparedState
{
    public static int Main(string[] args)
    {
        try
        {
            var state = new JavaScriptSerializer().Deserialize<GameState>(File.ReadAllLines(args[0]).Last());
            StateValidation.Check(state);
            Console.WriteLine("PASS prepared fixture validates against actual game rules: " + args[0]);
            return 0;
        }
        catch(Exception e) { Console.Error.WriteLine(e.Message); return 1; }
    }
}
