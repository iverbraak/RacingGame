using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Racers {

    /// <summary>
    /// A RaceTrack for Racers to race upon. Keeps track of each racer's position on the RaceTrack
    /// as well as manages each individual thread.
    /// <list type="bullet">
    /// <item>
    /// <term>Authors:</term>
    /// <description>Sela Davis</description>
    /// <description>Dave Schwartz</description>
    /// </item>
    /// </list>
    /// </summary>
    public partial class RaceTrack : Form {

        #region fields

        // GUI data:
        private const string ICON_IMAGE =   // default racer image filepath
            "..\\..\\embars.gif";
        private const string LINE_IMAGE =   // default line image filepath
            "..\\..\\black.bmp";
        private Image racerIcon;            // racer image
        private const int LINE_WIDTH = 4;   // width of each line
        private int frameWidth;             // width of main window based on race data
        private int frameHeight;            // height of main window based on race data
        private PictureBox[,] racerPics;    // GUI items that represent the racers

        // Racers and RaceTrack:
        private const int FRAMEFACTOR = 20; // racers need to travel this many distances * each width
        private const int TRACKS = 5;       // # of racetracks
        private const int RACERS = 3;       // # of racers per track
        private bool raceFinished = false;  // whether or not the race is complete

        // Threads:
        private Thread[,] raceThreads;      // threads that are racing: [track,racer]
        private Racer[,] racers;            // the actual racer objects
        private Thread controlThread;       // thread that updates the GUI

        #endregion

        #region constructor

        /// <summary>
        /// Construct racing area for racers and tracks.
        /// </summary>
        public RaceTrack() {

            // initial GUI settings:
            setUpGUI();

            // set up threads with racing information:
            raceThreads = new Thread[TRACKS, RACERS];
            racers = new Racer[TRACKS, RACERS];
            racerPics = new PictureBox[TRACKS, RACERS];

            // create racers, their threads, who follows who, and who's the last racer in each track
            for (int t = 0; t < TRACKS; t++) {     // each track
                for (int r = 0; r < RACERS; r++) { // each racer

                    // create a racer:
                    Racer racer = addRacer(t, r);

                    //if racer isn't first, racer needs to know who will be handing off ("previous"):
                    if (r > 0)
                    {
                        racer.follow(raceThreads[t, r-1]);
                    }
                    // create thread for racer who'll move:
                    raceThreads[t, r] = new Thread(racer.move);
                    
                    // indicate last racer in team to know who declares victory:
                     if (r == 2)
                     {
                         racer.LastRunner = true;
                     }
                } // end racers

            } // end tracks

            // update the GUI:
            controlThread = new Thread(updateGUI);
            controlThread.Start();

            // ensure each racing team starts as soon as possible by moving the 
            // first member of each time--other racers will join slightly later:
            for (int r = 0; r < RACERS; r++)
                for (int t = 0; t < TRACKS; t++)
                    raceThreads[t, r].Start();

            // reset main window to updated racing info:
            this.Width = frameWidth;
            this.Height = frameHeight;

        } // constructor RaceTrack

        /// <summary>
        /// Set up initial GUI data.
        /// </summary>
        private void setUpGUI() {

            // standard GUI set up:
            InitializeComponent();

            // Move the individual PictureBoxes directly from our racers:  
            Control.CheckForIllegalCrossThreadCalls = false;

            // load icon and set initial window size based on the icon:
            racerIcon = Image.FromFile(ICON_IMAGE);
            frameWidth = racerIcon.Width * FRAMEFACTOR;
            frameHeight = racerIcon.Height * 2 * TRACKS;

            // add lines in between teams:
            addLines();

        } // method setUpGUI

        /// <summary>
        /// Add lines to race. Last line is finish line.
        /// </summary>
        private void addLines() {
            for (int i = 0; i < RACERS; i++) {
                PictureBox line = new PictureBox();
                line.Image = Image.FromFile(LINE_IMAGE);
                line.Width = LINE_WIDTH;
                line.Height = this.Height;
                line.Location = new Point((this.Width / RACERS), 0);
                line.Location = new Point((line.Location.X * 2 * i) + (this.Width / RACERS) + racerIcon.Width, line.Location.Y);
                line.SizeMode = PictureBoxSizeMode.StretchImage;
                this.Controls.Add(line);
            }
        }

        #endregion

        #region racing

        /// <summary>
        /// Create a Racer given a track and position.
        /// </summary>
        /// <param name="trackPosition">what track ("the team") Racer will be on</param>
        /// <param name="racerPosition">which position on the track</param>
        /// <returns>the Racer</returns>
        private Racer addRacer(int trackPosition, int racerPosition) {
            PictureBox pic = new PictureBox();
            pic.Image = racerIcon;
            pic.Location = new Point((racerPosition * this.Width * 2) / RACERS, (trackPosition * this.Height) / TRACKS);
            pic.SizeMode = PictureBoxSizeMode.AutoSize;
            pic.BringToFront();
            this.Controls.Add(pic);
            Racer racer = new Racer(this, trackPosition, pic.Location.X, this.Width / RACERS);
            racers[trackPosition, racerPosition] = racer;
            racerPics[trackPosition, racerPosition] = pic;
            return racer;
        }

        /// <summary>
        /// Redraw racers during race.
        /// </summary>
        private void updateGUI() {
            while (!raceFinished) {
                for (int i = 0; i < TRACKS; i++) {
                    for (int j = 0; j < RACERS; j++) {
                        racerPics[i, j].Location = new Point(racers[i, j].X, racerPics[i, j].Location.Y);
                    }
                }
            }
        }

        /// <summary>
        /// Terminate the race and alert who won.
        /// </summary>
        public void raceComplete(int track) {
            raceFinished = true;
            abortAllThreads();
            MessageBox.Show("The team on track " + (track+1) + " has won!");
        }

        /// <summary>
        /// Stops any racer threads still running.
        /// </summary>
        private void abortAllThreads() {
            try {
                for (int i = 0; i < TRACKS; i++) {
                    for (int j = 0; j < RACERS; j++) {
                        // Make sure that the thread is both alive (no point in aborting otherwise!)
                        // and that it's not the current thread.
                        if (raceThreads[i, j].IsAlive && raceThreads[i, j] != Thread.CurrentThread) {
                            raceThreads[i, j].Abort();
                        }
                    }
                }
                if (controlThread.IsAlive) {
                    controlThread.Abort();
                }
            }
            catch (ThreadStateException) {
                System.Console.Error.WriteLine("Thread(s) could not be aborted.");
            }
        }

        #endregion

    } // class RaceTrack

} // namespace Racers
