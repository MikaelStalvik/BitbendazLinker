using BitbendazLinkerClient.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace BitbendazLinkerClient
{
    public static class Extensions
    {
        public static long GetFileSize(string filename)
        {
            if (File.Exists(filename))
                return new FileInfo(filename).Length;
            return 0;
        }

        public static List<T> ToList<T>(this ObservableCollection<T> source)
        {
            return new List<T>(source);
        }
        public static List<string> ToList(this ObservableCollection<FileHolder> source)
        {
            return source.Select(x => x.Filename).ToList();
        }
        public static ObservableCollection<FileHolder> ToFileHolder(this List<string> source)
        {
            var result = new ObservableCollection<FileHolder>();
            foreach(var file in source)
            {
                result.Add(new FileHolder { Filename = file, Size = GetFileSize(file) });
            }
            return result;
        }

        public static IEnumerable<string> ToListCheckFiles(this ObservableCollection<FileHolder> list)
        {
            var result = new List<string>();
            foreach (var item in list)
            {
                if (File.Exists(item.Filename))
                {
                    result.Add(item.Filename);
                }
            }
            return result;
        }

    }
}
