using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.Win
{
    public class ToastRequest
    {
        public int Version { get; set; }
        public string? Title { get; set; }
        public string? Text { get; set; }
        public string? Icon { get; set; }
    }
}
