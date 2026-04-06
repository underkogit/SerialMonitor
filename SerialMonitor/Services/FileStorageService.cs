using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace SerialMonitor.Services
{
    public class FileStorageService
    {
        private readonly string _filePath;

        public FileStorageService(string filePath = "Data\\commands.txt")
        {
            _filePath = filePath;
        }

        public void Save(ObservableCollection<string> data)
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllLines(_filePath, data);
        }

        public ObservableCollection<string> Load()
        {
            if (!File.Exists(_filePath))
                return new ObservableCollection<string>();

            var lines = File.ReadAllLines(_filePath);
            return new ObservableCollection<string>(lines);
        }
    }
}