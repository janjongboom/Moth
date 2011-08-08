using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Moth.Core.Helpers
{
    public interface IAssemblySearcher
    {
        /// <summary>
        /// Find all implementations of a given interface <see typeparam="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        List<Type> FindImplementations<T>();
    }

    /// <summary>
    /// Scans the currently loaded assembly's
    /// </summary>
    public class AssemblySearcher : IAssemblySearcher
    {
        /// <summary>
        /// Find all implementations of a given interface <see typeparam="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<Type> FindImplementations<T>()
        {
            List<Type> types = new List<Type>();
            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies()
                .Where(m=>!m.FullName.StartsWith("System") && !m.FullName.StartsWith("Microsoft")))
            {
                types.AddRange(from t in assembly.GetTypes()
                               where typeof(T).IsAssignableFrom(t) && t != typeof (T) && t.IsClass
                               select t);
            }
            return types;
        }
    }
}
