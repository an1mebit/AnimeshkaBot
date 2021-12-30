using System.Threading.Tasks;
using AnimeshkaBot.conf;

namespace AnimeshkaBot
{
    public class Program
    {
        protected static Task Main(string[] args) => new AnimeshkaClient().InitializeAsync();
    }
}
