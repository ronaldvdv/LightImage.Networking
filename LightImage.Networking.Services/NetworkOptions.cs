using System.Reflection;

namespace LightImage.Networking.Services
{
    /// <summary>
    /// General networking options.
    /// </summary>
    public class NetworkOptions
    {
        /// <summary>
        /// Configuration section for general network settings.
        /// </summary>
        public const string C_CONFIG_SECTION = "network";

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkOptions"/> class with default host.
        /// </summary>
        public NetworkOptions()
        {
            Component = Type = DefaultName;
        }

        /// <summary>
        /// Gets the default name for the node, which is based on the assembly name.
        /// </summary>
        public static string DefaultName
        {
            get
            {
                var assembly = Assembly.GetEntryAssembly();
                if (assembly?.GetName() == null)
                {
                    return "-";
                }

                return assembly.GetName().Name;
            }
        }

        /// <summary>
        /// Gets or sets the descriptive name of this component.
        /// </summary>
        public string Component { get; set; }

        /// <summary>
        /// Gets or sets the technical name for the type of node.
        /// </summary>
        public string Type { get; set; }
    }
}