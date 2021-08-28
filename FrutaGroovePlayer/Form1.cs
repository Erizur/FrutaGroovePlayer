using System;
using System.Reflection;
using NAudio.Wave;
using NAudio;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Taskbar;
using System.Windows.Interop;
using NAudio.CoreAudioApi;
using DiscordRPC;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Xml;

namespace FrutaGroovePlayer
{
    public partial class Form1 : Form
    {
        public string songPath;
        public string jsonPlaylist;
        public List<string> songList = new List<string>();
        public List<string> fileArray;
        public bool isMultipleSongs;
        public bool isPlaylist;
        public bool isSwitching;
        public WaveOutEvent outputDevice;
        public AudioFileReader audioFile;
        public ThumbnailToolBarButton tbPlayButton;
        public ThumbnailToolBarButton tbPauseButton;
        public int song_index = 0;
        public DiscordRpcClient Client { get; private set; }

        public Form1(string songLocation, bool willReload)
        {
            int en;

            if (willReload == true)
            {
                List<Process> processes = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).ToList();
                if (processes.Count > 1)
                {
                    processes.Sort((x, y) => DateTime.Compare(x.StartTime, y.StartTime));
                    for (int i = 0; i < processes.Count - 1; i++)
                    {
                        processes[i].CloseMainWindow();
                        processes[i].WaitForExit();
                    }
                }
            }

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string resourceName = new AssemblyName(args.Name).Name + ".dll";
                string resource = Array.Find(this.GetType().Assembly.GetManifestResourceNames(), element => element.EndsWith(resourceName));

                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
                {
                    Byte[] assemblyData = new Byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    return Assembly.Load(assemblyData);
                }
            };

            InitializeComponent();

            if (System.Environment.OSVersion.Version.Major < 6)  //make sure you are not on a legacy OS 
            {
                //MessageBox.Show("Why not try this on Vista?");
            }
            else
            {
                en = 0;
                MARGINS mg = new MARGINS();
                mg.m_Bottom = 0;
                mg.m_Left = 0;
                mg.m_Right = 0;
                mg.m_Top = pictureBox1.Height;


                DwmIsCompositionEnabled(ref en);  //check if the desktop composition is enabled
                if (en > 0)
                {
                    DwmExtendFrameIntoClientArea(this.Handle, ref mg);


                }
                else
                {
                    MessageBox.Show("DWM/Desktop Composition is currently disabled. FrutaGroovePlayer will not work correctly.");
                }

            }
            if (songLocation == null || songLocation == "")
            {

            }
            else
            {
                var multipleSongPath = Path.GetDirectoryName(songLocation);
                fileArray = Directory.EnumerateFiles(multipleSongPath, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".flac", StringComparison.OrdinalIgnoreCase)).ToList();
                if (fileArray.Count != 0)
                {
                    isMultipleSongs = true;
                }
                songPath = songLocation;
            }
            
        }

        #region DWM API
        public struct MARGINS
        {
            public int m_Left;
            public int m_Right;
            public int m_Top;
            public int m_Bottom;
        }
        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        public extern static int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margin);


        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        public extern static int DwmIsCompositionEnabled(ref int en);



        #endregion DWM API
        void SetupRPC()
        {
            Client = new DiscordRpcClient("863620431587704892");  //Creates the client
            Client.Initialize();                            //Connects the client
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void tbCheck_PlayState(object sender, EventArgs args)
        {

        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs args)
        {

            if (isPlaylist == true && audioFile.Position >= audioFile.Length && song_index != songList.Count - 1)
            {
                song_index++;
                songPath = songList[song_index];
                audioFile = new AudioFileReader(songPath);
                if (WaveOut.DeviceCount != 0)
                {
                    if (outputDevice == null)
                    {
                        outputDevice = new WaveOutEvent();
                        outputDevice.PlaybackStopped += OnPlaybackStopped;
                    }
                    string songNamePath = Path.GetFileName(songPath);
                    var coverFile = TagLib.File.Create(songPath);
                    audioFile.Position = 0;
                    trackBar2.Refresh();
                    timer1.Start();
                    this.Text = "FrutaGroove Player - " + songNamePath;
                    if (coverFile.Tag.Pictures.Length >= 1)
                    {
                        var bin = (byte[])(coverFile.Tag.Pictures[0].Data.Data);
                        pictureBox2.BackgroundImage = Image.FromStream(new MemoryStream(bin)).GetThumbnailImage(200, 200, null, IntPtr.Zero);
                    }
                    else
                    {
                        pictureBox2.BackgroundImage = Properties.Resources.nocoverNew;
                    }
                    outputDevice.Volume = trackBar1.Value / 100f;
                    outputDevice.Init(audioFile);
                    outputDevice.Play();
                    if (coverFile.Tag.Title != null && coverFile.Tag.FirstPerformer != null)
                    {
                        Client.SetPresence(new RichPresence()
                        {
                            Details = "Simple Music Player",
                            State = "Playing " + coverFile.Tag.Title + " by " + coverFile.Tag.FirstPerformer,
                            Assets = new Assets()
                            {
                                LargeImageKey = "image_large",
                                LargeImageText = "1.1",
                                SmallImageKey = "image_small"
                            }
                        });
                    }
                    else if (coverFile.Tag.Title != null)
                    {
                        Client.SetPresence(new RichPresence()
                        {
                            Details = "Simple Music Player",
                            State = "Playing " + coverFile.Tag.Title + " by an Unknown Artist",
                            Assets = new Assets()
                            {
                                LargeImageKey = "image_large",
                                LargeImageText = "1.1",
                                SmallImageKey = "image_small"
                            }
                        });
                    }
                    else
                    {
                        Client.SetPresence(new RichPresence()
                        {
                            Details = "Simple Music Player",
                            State = "Playing a Music File",
                            Assets = new Assets()
                            {
                                LargeImageKey = "image_large",
                                LargeImageText = "1.1",
                                SmallImageKey = "image_small"
                            }
                        });
                    }
                    PlayButton.Visible = false;
                    PauseButton.Visible = true;
                    tbPlayButton.Enabled = false;
                    tbPauseButton.Enabled = true;
                }
            }
            else if (isPlaylist == true && isSwitching == true || isMultipleSongs == true && isSwitching == true)
            {
                isSwitching = false;
                PlayButton.Visible = false;
                PauseButton.Visible = true;
                tbPlayButton.Enabled = false;
                tbPauseButton.Enabled = true;
            }
            else if (isPlaylist == true && audioFile.Position >= audioFile.Length && song_index == songList.Count - 1)
            {
                if (audioFile == null)
                {

                }
                else
                {
                    audioFile.Position = 0;
                    PlayButton.Visible = true;
                    PauseButton.Visible = false;
                    tbPlayButton.Enabled = true;
                    tbPauseButton.Enabled = false;
                    this.Text = "FrutaGroove Player";
                    timer1.Stop();
                    if (outputDevice == null)
                    {
                        outputDevice.Dispose();
                        outputDevice = null;
                    }
                }
            }
            else
            {
                PlayButton.Visible = true;
                PauseButton.Visible = false;
                tbPlayButton.Enabled = true;
                tbPauseButton.Enabled = false;
                this.Text = "FrutaGroove Player";
                timer1.Stop();
                if (outputDevice == null)
                {
                    outputDevice.Dispose();
                    outputDevice = null;
                }
            }
            
        }

        private void PauseButton_Click(object sender, EventArgs e)
        {
            if(isPlaylist == true && song_index == songList.Count - 1)
            {

            }
            if (WaveOut.DeviceCount != 0)
            {
                outputDevice.Stop();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            tbPlayButton = new ThumbnailToolBarButton(Properties.Resources.playSymbol1, "Play");
            tbPauseButton = new ThumbnailToolBarButton(Properties.Resources.pauseSymbol1, "Pause");
            tbPlayButton.Enabled = true;
            tbPauseButton.Enabled = false;
            tbPlayButton.Click += new EventHandler<ThumbnailButtonClickedEventArgs>(PlayButton_Click);
            tbPauseButton.Click += new EventHandler<ThumbnailButtonClickedEventArgs>(PauseButton_Click);
            TaskbarManager.Instance.ThumbnailToolBars.AddButtons(this.Handle, tbPlayButton, tbPauseButton);
            SetupRPC();
            if (Properties.Settings.Default.updateStartup == true)
            {
                CheckForUpdate();
            }
            if (songPath == null || songPath == "")
            {

            }
            else
            {
                audioFile = new AudioFileReader(songPath);
                if (WaveOut.DeviceCount != 0)
                {
                    if (outputDevice == null)
                    {
                        outputDevice = new WaveOutEvent();
                        outputDevice.PlaybackStopped += OnPlaybackStopped;
                    }
                    string songNamePath = Path.GetFileName(songPath);
                    var coverFile = TagLib.File.Create(songPath);
                    timer1.Start();
                    this.Text = "FrutaGroove Player - " + songNamePath;
                    if (coverFile.Tag.Pictures.Length >= 1)
                    {
                        var bin = (byte[])(coverFile.Tag.Pictures[0].Data.Data);
                        pictureBox2.BackgroundImage = Image.FromStream(new MemoryStream(bin)).GetThumbnailImage(200, 200, null, IntPtr.Zero);
                    }
                    outputDevice.Volume = trackBar1.Value / 100f;
                    outputDevice.Init(audioFile);
                    outputDevice.Play();
                    if (coverFile.Tag.Title != null && coverFile.Tag.FirstPerformer != null)
                    {
                        Client.SetPresence(new RichPresence()
                        {
                            Details = "Simple Music Player",
                            State = "Playing " + coverFile.Tag.Title + " by " + coverFile.Tag.FirstPerformer,
                            Assets = new Assets()
                            {
                                LargeImageKey = "image_large",
                                LargeImageText = "1.1",
                                SmallImageKey = "image_small"
                            }
                        });
                    }
                    else if (coverFile.Tag.Title != null)
                    {
                        Client.SetPresence(new RichPresence()
                        {
                            Details = "Simple Music Player",
                            State = "Playing " + coverFile.Tag.Title + " by an Unknown Artist",
                            Assets = new Assets()
                            {
                                LargeImageKey = "image_large",
                                LargeImageText = "1.1",
                                SmallImageKey = "image_small"
                            }
                        });
                    }
                    else
                    {
                        Client.SetPresence(new RichPresence()
                        {
                            Details = "Simple Music Player",
                            State = "Playing a Music File",
                            Assets = new Assets()
                            {
                                LargeImageKey = "image_large",
                                LargeImageText = "1.1",
                                SmallImageKey = "image_small"
                            }
                        });
                    }
                    PlayButton.Visible = false;
                    PauseButton.Visible = true;
                    tbPlayButton.Enabled = false;
                    tbPauseButton.Enabled = true;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (audioFile == null)
            {

            }
            else
            {
                audioFile.Position = 0;
                trackBar2.Value = 0;
                if (WaveOut.DeviceCount != 0 && outputDevice != null)
                {
                    outputDevice.Stop();
                }
                PlayButton.Visible = true;
                PauseButton.Visible = false;
                tbPlayButton.Enabled = true;
                tbPauseButton.Enabled = false;
            }

        }


        private void button1_Click_1(object sender, EventArgs e)
        {
            if (audioFile != null)
            {
                if (outputDevice != null)
                {
                    if (WaveOut.DeviceCount != 0)
                    {
                        outputDevice.Stop();
                    }
                }
                pictureBox2.BackgroundImage = Properties.Resources.nocoverNew;
                audioFile.Position = 0;
                PlayButton.Visible = true;
                isMultipleSongs = false;
                song_index = 0;
                songPath = null;
                if (isPlaylist == true)
                {
                    isPlaylist = false;
                    songList.Clear();
                }
                PauseButton.Visible = false;
                tbPlayButton.Enabled = true;
                tbPauseButton.Enabled = false;
            }
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                songPath = openFileDialog1.FileName;
                audioFile = new AudioFileReader(songPath);
            }
        }

        public void PlayButton_Click(object sender, EventArgs e)
        {

            if (WaveOut.DeviceCount != 0)
            {
                if (outputDevice == null)
                {
                    outputDevice = new WaveOutEvent();
                    outputDevice.PlaybackStopped += OnPlaybackStopped;
                }
                if (audioFile == null)
                {
                    MessageBox.Show("Please select an Audio File.", "No Audio File Detected", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    string songNamePath = Path.GetFileName(songPath);
                    timer1.Start();
                    this.Text = "FrutaGroove Player - " + songNamePath;
                    outputDevice.Volume = trackBar1.Value / 100f;
                    outputDevice.Init(audioFile);
                    outputDevice.Play();
                    var coverFile = TagLib.File.Create(songPath);
                    if (coverFile.Tag.Title != null && coverFile.Tag.FirstPerformer != null)
                    {
                        Client.SetPresence(new RichPresence()
                        {
                            Details = "Simple Music Player",
                            State = "Playing " + coverFile.Tag.Title + " by " + coverFile.Tag.FirstPerformer,
                            Assets = new Assets()
                            {
                                LargeImageKey = "image_large",
                                LargeImageText = "1.1",
                                SmallImageKey = "image_small"
                            }
                        });
                    }
                    else if (coverFile.Tag.Title != null)
                    {
                        Client.SetPresence(new RichPresence()
                        {
                            Details = "Simple Music Player",
                            State = "Playing " + coverFile.Tag.Title + " by an Unknown Artist",
                            Assets = new Assets()
                            {
                                LargeImageKey = "image_large",
                                LargeImageText = "1.1",
                                SmallImageKey = "image_small"
                            }
                        });
                    }
                    else
                    {
                        Client.SetPresence(new RichPresence()
                        {
                            Details = "Simple Music Player",
                            State = "Playing a Music File",
                            Assets = new Assets()
                            {
                                LargeImageKey = "image_large",
                                LargeImageText = "1.1",
                                SmallImageKey = "image_small"
                            }
                        });
                    }
                    if (coverFile.Tag.Pictures.Length >= 1)
                    {
                        var bin = (byte[])(coverFile.Tag.Pictures[0].Data.Data);
                        pictureBox2.BackgroundImage = Image.FromStream(new MemoryStream(bin)).GetThumbnailImage(200, 200, null, IntPtr.Zero);
                    }
                    PlayButton.Visible = false;
                    PauseButton.Visible = true;
                    tbPlayButton.Enabled = false;
                    tbPauseButton.Enabled = true;
                }
            }
            else
            {
                MessageBox.Show("Connect an Audio Device.", "No Audio Device Detected", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (audioFile != null)
            {
                var maxTime = audioFile.Length;
                var currentTime = audioFile.Position;
                int currentTimeInt = Convert.ToInt32(currentTime);
                int maxTimeInt = Convert.ToInt32(maxTime);
                trackBar2.Maximum = maxTimeInt + 700000;
                trackBar2.Value = currentTimeInt;
                if (trackBar2.Value >= trackBar2.Maximum || currentTimeInt >= trackBar2.Maximum)
                {
                    if(WaveOut.DeviceCount != 0)
                    {
                        outputDevice.Stop();
                    }
                    trackBar2.Value = 0;
                    currentTimeInt = 0;
                    audioFile.Position = 0;

                }
            }
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {

            if (audioFile != null)
            {
                audioFile.Position = trackBar2.Value;
                if(WaveOut.DeviceCount != 0)
                {
                    outputDevice.Stop();
                } 
                if (trackBar2.Value >= trackBar2.Maximum)
                {
                    audioFile.Position = trackBar2.Value - 40000;
                    outputDevice.Stop();
                }
            }

        }

        private void trackBar2_MouseUp(object sender, MouseEventArgs e)
        {
            if (audioFile != null)
            {
                if (WaveOut.DeviceCount != 0)
                {
                    if (trackBar2.Value >= trackBar2.Maximum)
                    {
                        outputDevice.Stop();
                    }
                    else
                    {
                        timer1.Start();
                        outputDevice.Play();
                        string songNamePath = Path.GetFileName(songPath);
                        this.Text = "FrutaGroove Player - " + songNamePath;
                        PlayButton.Visible = false;
                        PauseButton.Visible = true;
                        tbPlayButton.Enabled = false;
                        tbPauseButton.Enabled = true;
                    }

                }
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            if (outputDevice != null)
            {
                if (WaveOut.DeviceCount != 0)
                {
                    outputDevice.Volume = trackBar1.Value / 100f;
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (audioFile != null)
            {
                if (outputDevice != null)
                {
                    if (WaveOut.DeviceCount != 0)
                    {
                        outputDevice.Stop();
                    }
                }
            }
            if(isPlaylist == true)
            {
                isPlaylist = false;
                isMultipleSongs = false;
                songList.Clear();
                song_index = 0;
                songPath = null;
            }
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                StreamReader r = new StreamReader(openFileDialog2.FileName);
                string jsonString = r.ReadToEnd();
                List<PlaylistReader> Song = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlaylistReader>>(jsonString);
                foreach (PlaylistReader obj in Song)
                {
                    string[] str = obj.Song.Split(',');
                    foreach (string songEntity in str)
                    {
                        songList.Add(songEntity);
                    }
                }
                song_index = 0;
                songPath = songList[song_index];
                audioFile = new AudioFileReader(songPath);
                isPlaylist = true;
            }
            
        }
        public void PlaylistStuff()
        {
            if(songList.Count >= 1)
            {
                if (song_index > songList.Count || songList.Count > song_index)
                {
                    if (WaveOut.DeviceCount != 0)
                    {
                        outputDevice.Stop();
                    }
                    song_index = 0;
                    isPlaylist = false;
                    trackBar2.Value = 0;
                    audioFile.Position = 0;
                }
                else
                {
                    songPath = songList[song_index];
                    audioFile = new AudioFileReader(songPath);
                    if (WaveOut.DeviceCount != 0)
                    {
                        if (outputDevice == null)
                        {
                            outputDevice = new WaveOutEvent();
                            outputDevice.PlaybackStopped += OnPlaybackStopped;
                        }
                        string songNamePath = Path.GetFileName(songPath);
                        var coverFile = TagLib.File.Create(songPath);
                        trackBar2.Value = 0;
                        audioFile.Position = 0;
                        timer1.Start();
                        this.Text = "FrutaGroove Player - " + songNamePath;
                        if (coverFile.Tag.Pictures.Length >= 1)
                        {
                            var bin = (byte[])(coverFile.Tag.Pictures[0].Data.Data);
                            pictureBox2.BackgroundImage = Image.FromStream(new MemoryStream(bin)).GetThumbnailImage(200, 200, null, IntPtr.Zero);
                        }
                        outputDevice.Volume = trackBar1.Value / 100f;
                        outputDevice.Init(audioFile);
                        outputDevice.Play();
                        if (coverFile.Tag.Title != null && coverFile.Tag.FirstPerformer != null)
                        {
                            Client.SetPresence(new RichPresence()
                            {
                                Details = "Simple Music Player",
                                State = "Playing " + coverFile.Tag.Title + " by " + coverFile.Tag.FirstPerformer,
                                Assets = new Assets()
                                {
                                    LargeImageKey = "image_large",
                                    LargeImageText = "1.1",
                                    SmallImageKey = "image_small"
                                }
                            });
                        }
                        else if (coverFile.Tag.Title != null)
                        {
                            Client.SetPresence(new RichPresence()
                            {
                                Details = "Simple Music Player",
                                State = "Playing " + coverFile.Tag.Title + " by an Unknown Artist",
                                Assets = new Assets()
                                {
                                    LargeImageKey = "image_large",
                                    LargeImageText = "1.1",
                                    SmallImageKey = "image_small"
                                }
                            });
                        }
                        else
                        {
                            Client.SetPresence(new RichPresence()
                            {
                                Details = "Simple Music Player",
                                State = "Playing a Music File",
                                Assets = new Assets()
                                {
                                    LargeImageKey = "image_large",
                                    LargeImageText = "1.1",
                                    SmallImageKey = "image_small"
                                }
                            });
                        }
                        PlayButton.Visible = false;
                        PauseButton.Visible = true;
                        tbPlayButton.Enabled = false;
                        tbPauseButton.Enabled = true;
                    }
                }
            }
            else
            {
                if (WaveOut.DeviceCount != 0)
                {
                    outputDevice.Stop();
                }
            }
            
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if(isMultipleSongs == true)
            {
                if (song_index == fileArray.Count - 1)
                {
                    if (audioFile == null)
                    {

                    }
                    else
                    {
                        PlaylistEnd();
                    }
                }
                else
                {
                    song_index++;
                    songPath = fileArray[song_index];
                    audioFile = new AudioFileReader(songPath);
                }
                if (WaveOut.DeviceCount != 0)
                {
                    if (outputDevice == null)
                    {
                        outputDevice = new WaveOutEvent();
                        outputDevice.PlaybackStopped += OnPlaybackStopped;
                    }
                    string songNamePath = Path.GetFileName(songPath);
                    var coverFile = TagLib.File.Create(songPath);
                    this.Text = "FrutaGroove Player - " + songNamePath;
                    if (coverFile.Tag.Pictures.Length >= 1)
                    {
                        var bin = (byte[])(coverFile.Tag.Pictures[0].Data.Data);
                        pictureBox2.BackgroundImage = Image.FromStream(new MemoryStream(bin)).GetThumbnailImage(200, 200, null, IntPtr.Zero);
                    }
                    else
                    {
                        pictureBox2.BackgroundImage = Properties.Resources.nocoverNew;
                    }
                    outputDevice.Volume = trackBar1.Value / 100f;
                    if (outputDevice.PlaybackState == PlaybackState.Playing)
                    {
                        isSwitching = true;
                        outputDevice.Stop();
                        audioFile.Position = 0;
                        outputDevice.Init(audioFile);
                        outputDevice.Play();
                        trackBar2.Refresh();
                        timer1.Start();
                    }
                    else
                    {
                        audioFile.Position = 0;
                        outputDevice.Init(audioFile);
                        outputDevice.Play();
                        trackBar2.Refresh();
                        timer1.Start();
                    }
                    if (coverFile.Tag.Title != null && coverFile.Tag.FirstPerformer != null)
                    {
                        Client.SetPresence(new RichPresence()
                        {
                            Details = "Simple Music Player",
                            State = "Playing " + coverFile.Tag.Title + " by " + coverFile.Tag.FirstPerformer,
                            Assets = new Assets()
                            {
                                LargeImageKey = "image_large",
                                LargeImageText = "1.1",
                                SmallImageKey = "image_small"
                            }
                        });
                    }
                    else if (coverFile.Tag.Title != null)
                    {
                        Client.SetPresence(new RichPresence()
                        {
                            Details = "Simple Music Player",
                            State = "Playing " + coverFile.Tag.Title + " by an Unknown Artist",
                            Assets = new Assets()
                            {
                                LargeImageKey = "image_large",
                                LargeImageText = "1.1",
                                SmallImageKey = "image_small"
                            }
                        });
                    }
                    else
                    {
                        Client.SetPresence(new RichPresence()
                        {
                            Details = "Simple Music Player",
                            State = "Playing a Music File",
                            Assets = new Assets()
                            {
                                LargeImageKey = "image_large",
                                LargeImageText = "1.1",
                                SmallImageKey = "image_small"
                            }
                        });
                    }
                    PlayButton.Visible = false;
                    PauseButton.Visible = true;
                    tbPlayButton.Enabled = false;
                    tbPauseButton.Enabled = true;
                }
            }
            if (isPlaylist == true)
            {
                if(song_index >= songList.Count)
                {

                }
                else
                {
                    if(song_index == songList.Count - 1)
                    {
                        if (audioFile == null)
                        {

                        }
                        else
                        {
                            PlaylistEnd();
                        }
                    }
                    else
                    {
                        song_index++;
                        songPath = songList[song_index];
                        audioFile = new AudioFileReader(songPath);
                    }
                    if (WaveOut.DeviceCount != 0)
                    {
                        if (outputDevice == null)
                        {
                            outputDevice = new WaveOutEvent();
                            outputDevice.PlaybackStopped += OnPlaybackStopped;
                        }
                        string songNamePath = Path.GetFileName(songPath);
                        var coverFile = TagLib.File.Create(songPath);
                        this.Text = "FrutaGroove Player - " + songNamePath;
                        if (coverFile.Tag.Pictures.Length >= 1)
                        {
                            var bin = (byte[])(coverFile.Tag.Pictures[0].Data.Data);
                            pictureBox2.BackgroundImage = Image.FromStream(new MemoryStream(bin)).GetThumbnailImage(200, 200, null, IntPtr.Zero);
                        }
                        else
                        {
                            pictureBox2.BackgroundImage = Properties.Resources.nocoverNew;
                        }
                        outputDevice.Volume = trackBar1.Value / 100f;
                        if (outputDevice.PlaybackState == PlaybackState.Playing)
                        {
                            isSwitching = true;
                            outputDevice.Stop();
                            audioFile.Position = 0;
                            outputDevice.Init(audioFile);
                            outputDevice.Play();
                            trackBar2.Refresh();
                            timer1.Start();
                        }
                        else
                        {
                            audioFile.Position = 0;
                            outputDevice.Init(audioFile);
                            outputDevice.Play();
                            trackBar2.Refresh();
                            timer1.Start();
                        }
                        if (coverFile.Tag.Title != null && coverFile.Tag.FirstPerformer != null)
                        {
                            Client.SetPresence(new RichPresence()
                            {
                                Details = "Simple Music Player",
                                State = "Playing " + coverFile.Tag.Title + " by " + coverFile.Tag.FirstPerformer,
                                Assets = new Assets()
                                {
                                    LargeImageKey = "image_large",
                                    LargeImageText = "1.1",
                                    SmallImageKey = "image_small"
                                }
                            });
                        }
                        else if (coverFile.Tag.Title != null)
                        {
                            Client.SetPresence(new RichPresence()
                            {
                                Details = "Simple Music Player",
                                State = "Playing " + coverFile.Tag.Title + " by an Unknown Artist",
                                Assets = new Assets()
                                {
                                    LargeImageKey = "image_large",
                                    LargeImageText = "1.1",
                                    SmallImageKey = "image_small"
                                }
                            });
                        }
                        else
                        {
                            Client.SetPresence(new RichPresence()
                            {
                                Details = "Simple Music Player",
                                State = "Playing a Music File",
                                Assets = new Assets()
                                {
                                    LargeImageKey = "image_large",
                                    LargeImageText = "1.1",
                                    SmallImageKey = "image_small"
                                }
                            });
                        }
                        PlayButton.Visible = false;
                        PauseButton.Visible = true;
                        tbPlayButton.Enabled = false;
                        tbPauseButton.Enabled = true;
                    }
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if(isMultipleSongs == true)
            {
                if (song_index <= 0)
                {
                    outputDevice.Stop();
                }
                else
                {
                    song_index--;
                    songPath = fileArray[song_index];
                    audioFile = new AudioFileReader(songPath);
                    if (WaveOut.DeviceCount != 0)
                    {
                        if (outputDevice == null)
                        {
                            outputDevice = new WaveOutEvent();
                            outputDevice.PlaybackStopped += OnPlaybackStopped;
                        }
                        string songNamePath = Path.GetFileName(songPath);
                        var coverFile = TagLib.File.Create(songPath);
                        this.Text = "FrutaGroove Player - " + songNamePath;
                        if (coverFile.Tag.Pictures.Length >= 1)
                        {
                            var bin = (byte[])(coverFile.Tag.Pictures[0].Data.Data);
                            pictureBox2.BackgroundImage = Image.FromStream(new MemoryStream(bin)).GetThumbnailImage(200, 200, null, IntPtr.Zero);
                        }
                        else
                        {
                            pictureBox2.BackgroundImage = Properties.Resources.nocoverNew;
                        }
                        outputDevice.Volume = trackBar1.Value / 100f;
                        if (outputDevice.PlaybackState == PlaybackState.Playing)
                        {
                            isSwitching = true;
                            outputDevice.Stop();
                            audioFile.Position = 0;
                            outputDevice.Init(audioFile);
                            outputDevice.Play();
                            trackBar2.Refresh();
                            timer1.Start();
                        }
                        else
                        {
                            audioFile.Position = 0;
                            outputDevice.Init(audioFile);
                            outputDevice.Play();
                            trackBar2.Refresh();
                            timer1.Start();
                        }

                        if (coverFile.Tag.Title != null && coverFile.Tag.FirstPerformer != null)
                        {
                            Client.SetPresence(new RichPresence()
                            {
                                Details = "Simple Music Player",
                                State = "Playing " + coverFile.Tag.Title + " by " + coverFile.Tag.FirstPerformer,
                                Assets = new Assets()
                                {
                                    LargeImageKey = "image_large",
                                    LargeImageText = "1.1",
                                    SmallImageKey = "image_small"
                                }
                            });
                        }
                        else if (coverFile.Tag.Title != null)
                        {
                            Client.SetPresence(new RichPresence()
                            {
                                Details = "Simple Music Player",
                                State = "Playing " + coverFile.Tag.Title + " by an Unknown Artist",
                                Assets = new Assets()
                                {
                                    LargeImageKey = "image_large",
                                    LargeImageText = "1.1",
                                    SmallImageKey = "image_small"
                                }
                            });
                        }
                        else
                        {
                            Client.SetPresence(new RichPresence()
                            {
                                Details = "Simple Music Player",
                                State = "Playing a Music File",
                                Assets = new Assets()
                                {
                                    LargeImageKey = "image_large",
                                    LargeImageText = "1.1",
                                    SmallImageKey = "image_small"
                                }
                            });
                        }
                        PlayButton.Visible = false;
                        PauseButton.Visible = true;
                        tbPlayButton.Enabled = false;
                        tbPauseButton.Enabled = true;
                    }
                }
            }
            if (isPlaylist == true)
            {
                if(song_index <= 0)
                {

                }
                else
                {
                    song_index--;
                    songPath = songList[song_index];
                    audioFile = new AudioFileReader(songPath);
                    if (WaveOut.DeviceCount != 0)
                    {
                        if (outputDevice == null)
                        {
                            outputDevice = new WaveOutEvent();
                            outputDevice.PlaybackStopped += OnPlaybackStopped;
                        }
                        string songNamePath = Path.GetFileName(songPath);
                        var coverFile = TagLib.File.Create(songPath);
                        this.Text = "FrutaGroove Player - " + songNamePath;
                        if (coverFile.Tag.Pictures.Length >= 1)
                        {
                            var bin = (byte[])(coverFile.Tag.Pictures[0].Data.Data);
                            pictureBox2.BackgroundImage = Image.FromStream(new MemoryStream(bin)).GetThumbnailImage(200, 200, null, IntPtr.Zero);
                        }
                        else
                        {
                            pictureBox2.BackgroundImage = Properties.Resources.nocoverNew;
                        }
                        outputDevice.Volume = trackBar1.Value / 100f;
                        if(outputDevice.PlaybackState == PlaybackState.Playing)
                        {
                            isSwitching = true;
                            outputDevice.Stop();
                            audioFile.Position = 0;
                            outputDevice.Init(audioFile);
                            outputDevice.Play();
                            trackBar2.Refresh();
                            timer1.Start();
                        }
                        else
                        {
                            audioFile.Position = 0;
                            outputDevice.Init(audioFile);
                            outputDevice.Play();
                            trackBar2.Refresh();
                            timer1.Start();
                        }
                        
                        if (coverFile.Tag.Title != null && coverFile.Tag.FirstPerformer != null)
                        {
                            Client.SetPresence(new RichPresence()
                            {
                                Details = "Simple Music Player",
                                State = "Playing " + coverFile.Tag.Title + " by " + coverFile.Tag.FirstPerformer,
                                Assets = new Assets()
                                {
                                    LargeImageKey = "image_large",
                                    LargeImageText = "1.1",
                                    SmallImageKey = "image_small"
                                }
                            });
                        }
                        else if (coverFile.Tag.Title != null)
                        {
                            Client.SetPresence(new RichPresence()
                            {
                                Details = "Simple Music Player",
                                State = "Playing " + coverFile.Tag.Title + " by an Unknown Artist",
                                Assets = new Assets()
                                {
                                    LargeImageKey = "image_large",
                                    LargeImageText = "1.1",
                                    SmallImageKey = "image_small"
                                }
                            });
                        }
                        else
                        {
                            Client.SetPresence(new RichPresence()
                            {
                                Details = "Simple Music Player",
                                State = "Playing a Music File",
                                Assets = new Assets()
                                {
                                    LargeImageKey = "image_large",
                                    LargeImageText = "1.1",
                                    SmallImageKey = "image_small"
                                }
                            });
                        }
                        PlayButton.Visible = false;
                        PauseButton.Visible = true;
                        tbPlayButton.Enabled = false;
                        tbPauseButton.Enabled = true;
                    }
                }
                
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Form2 fm2 = new Form2();
            fm2.Show();
        }

        private void PlaylistEnd()
        {
            if (audioFile == null)
            {

            }
            else
            {
                audioFile.Position = 0;
                PlayButton.Visible = true;
                PauseButton.Visible = false;
                tbPlayButton.Enabled = true;
                tbPauseButton.Enabled = false;
                this.Text = "FrutaGroove Player";
                timer1.Stop();
                if (outputDevice == null)
                {
                    outputDevice.Dispose();
                    outputDevice = null;
                }
            }
        }

        private void CheckForUpdate()
        {
            string downloadUrl = "";
            Version newVer = null;
            string xmlUrl = "https://am-games.net/fgp/updater.xml";
            XmlTextReader reader = null;
            try
            {
                reader = new XmlTextReader(xmlUrl);
                reader.MoveToContent();
                string elementName = "";
                if ((reader.NodeType == XmlNodeType.Element) && (reader.Name == "frutaGroove"))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            elementName = reader.Name;
                        }
                        else
                        {
                            if ((reader.NodeType == XmlNodeType.Text) && (reader.HasValue))
                            {
                                switch (elementName)
                                {
                                    case "version":
                                        newVer = new Version(reader.Value);
                                        break;
                                    case "url":
                                        downloadUrl = reader.Value;
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
                Version applicationVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                if (applicationVersion.CompareTo(newVer) < 0)
                {
                    DialogResult result = MessageBox.Show("FrutaGroove Player Version " + newVer.Major + "." + newVer.Minor + "." + newVer.Build + " is now available to download. Update Now?", "New Version Available", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        string path = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);

                        Process.Start(path + "\\FGPUpdater.exe");
                        Application.Exit();
                    }
                    else if (result == DialogResult.No)
                    {

                    }
                }
            }
        }

        private void button1_MouseHover(object sender, EventArgs e)
        {
            toolTip1.Show("Load File...", button1);
        }

        private void button3_MouseHover(object sender, EventArgs e)
        {
            toolTip1.Show("Load Playlist...", button3);
        }

        private void button5_MouseHover(object sender, EventArgs e)
        {
            toolTip1.Show("Next Song", button5);
        }

        private void button4_MouseHover(object sender, EventArgs e)
        {
            toolTip1.Show("Previous Song", button4);
        }

        private void button2_MouseHover(object sender, EventArgs e)
        {
            toolTip1.Show("Stop", button2);
        }

        private void PauseButton_MouseHover(object sender, EventArgs e)
        {
            toolTip1.Show("Pause", PauseButton);
        }

        private void PlayButton_MouseHover(object sender, EventArgs e)
        {
            toolTip1.Show("Play", PlayButton);
        }

        private void button6_MouseHover(object sender, EventArgs e)
        {
            toolTip1.Show("Settings", button6);
        }
    }
}
