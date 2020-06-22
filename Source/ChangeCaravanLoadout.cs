using HarmonyLib;
using System.Reflection;
using Verse;

namespace ChangeCaravanLoadout
{
    [StaticConstructorOnStartup]
    public class ChangeCaravanLoadout
    {
        static ChangeCaravanLoadout()
        {
            new Harmony("ChangeCaravanLoadout").PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
