using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestGrains
{
    public static class EnumerableEx
    {
        public static IEnumerable<T> NullSafe<T>(this IEnumerable<T> enumerable)
        {
            return enumerable ?? Enumerable.Empty<T>();
        }
    }

    public static class TasksEx
    {
        public static async Task Ensure(this Func<Task> work, int retries = 10, int delay = 100)
        {
            int tried = 0;

            while (true)
            {
                try
                {
                    await work();
                    return;
                }
                catch (Exception)
                {
                    if(tried >= retries) throw;
                }

                tried++;
            }
        }
        public static async Task<T> Ensure<T>(this Func<Task<T>> work, int retries = 10, int delay = 100)
        {
            int tried = 0;

            while (true)
            {
                try
                {
                    var res = await work();
                    return res;
                }
                catch (Exception)
                {
                    if(tried >= retries) throw;
                }

                tried++;
            }
        }
    }
}
