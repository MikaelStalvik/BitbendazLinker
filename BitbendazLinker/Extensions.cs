using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BitbendazLinker
{
    public static class Extensions
    {
        public static List<T> ToList<T>(this ObservableCollection<T> source)
        {
            return new List<T>(source);
        }
    }
}
