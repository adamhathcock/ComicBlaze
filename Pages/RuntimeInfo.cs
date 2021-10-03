using System.Reflection;
using System.Runtime.Versioning;

namespace ComicBlaze.Pages
{
    public record RuntimeInfo(string? Version, string? Framework, string OSDescription,
        string RuntimeIdentifier, string FrameworkDescription)
    {
        public static RuntimeInfo GetInfo()
        {
            var assembly = Assembly.GetExecutingAssembly();
            
            return new(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
                assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName,
                System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier,
                System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
        }
    }
}