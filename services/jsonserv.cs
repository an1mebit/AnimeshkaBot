using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using AnimeshkaBot.conf;

namespace AnimeshkaBot.services
{
    public class jsonserv
    {
        public static string Path { get; set; }
        public static config Config { get; set; }

        public jsonserv()
        {
            Path = "config.json";
        }

        public async Task InitializeAsync()
        {
            if (!File.Exists(Path))
            {
                await CreateFile();
            }

            string data = await File.ReadAllTextAsync(Path);
            Config = JsonConvert.DeserializeObject<config>(data);
        }

        private static Task CreateFile()
        {
            config config = new()
            {
                Token = "There is token",
                Prefix = "!",
                GameStatus = "Genshin Impact"
            };

            string data = JsonConvert.SerializeObject(config);
            File.WriteAllText(Path, data);
            return Task.CompletedTask;
        }
    }
}
