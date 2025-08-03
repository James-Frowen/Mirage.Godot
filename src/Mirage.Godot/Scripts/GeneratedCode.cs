using System;
using System.Reflection;
using Mirage.Logging;

namespace Mirage
{
    public static class GeneratedCode
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(GeneratedCode));

        public const string GENERATED_NAMEPACE = "_MirageGenerated";
        public const string GENERATED_CLASS = "GeneratedNetworkCode";
        public const string INIT_METHOD = "InitReadWriters";
        private static bool hasInit = false;

        public static void Init()
        {
            if (hasInit)
                return;

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    logger.LogWarning($"Could not load types from assembly: {assembly.FullName}, skipping.");
                    foreach (var loaderException in e.LoaderExceptions)
                    {
                        if (loaderException != null)
                            logger.LogWarning($"  LoaderException: {loaderException.Message}");
                    }
                    continue;
                }

                foreach (var type in types)
                {
                    if (type.Namespace != GENERATED_NAMEPACE || type.Name != GENERATED_CLASS)
                        continue;

                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    {
                        if (method.Name != INIT_METHOD)
                            continue;

                        if (logger.LogEnabled()) logger.Log($"Init Generated code in {assembly.FullName}");
                        method.Invoke(null, null);
                    }
                }
            }

            hasInit = true;
        }
    }
}
