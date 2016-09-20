using System;
using System.Windows.Forms;

namespace Racers {

    /// <summary>
    /// 
    /// </summary>
    static class Program {

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(String[] args) {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new RaceTrack());
        }
    }
}
