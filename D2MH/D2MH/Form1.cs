using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace D2RAssist
{
    public partial class Form1 : Form
    {
        private static readonly HttpClient client = new HttpClient();
        private Font fnt = new Font("Arial", 10);
        private GameData lastGameData = null;
        private GameData currentGameData = null;
        private SessionData mapApiSession;
        private MapData mapData;
        private Bitmap background;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // AllocConsole();
            // AttachConsole(-1);
            //Console.WriteLine("D2R Assist");
            //MapSeedReader.GetMapSeed();

            Timer GameDataTimer = new Timer();
            GameDataTimer.Interval = 1000;
            GameDataTimer.Tick += new EventHandler(GameDataTimer_Tick);
            GameDataTimer.Start();

        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        private async void GameDataTimer_Tick(object sender, EventArgs e)
        {
            currentGameData = MapSeedReader.GetMapSeed();

            if (currentGameData != null)
            {

                if (lastGameData?.MapSeed != currentGameData.MapSeed && currentGameData.MapSeed != 0)
                {
                    if (mapApiSession != null)
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            HttpResponseMessage response = await client.DeleteAsync("http://localhost:8080/sessions/" + mapApiSession.id);
                            mapApiSession = null;
                            background = null;
                            mapData = null;
                        }
                    }

                    lastGameData = currentGameData;

                    var values = new Dictionary<string, uint>
                    {
                        // { "id", "1" },
                        {"difficulty",2},
                        {"mapid", currentGameData.MapSeed}
                    };

                    using (HttpClient client = new HttpClient())
                    {
                        var json = JsonConvert.SerializeObject(values);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");
                        Console.WriteLine(json);
                        Console.WriteLine(content);
                        HttpResponseMessage response = await client.PostAsync("http://localhost:8080/sessions/", content);
                        this.mapApiSession = JsonConvert.DeserializeObject<SessionData>(await response.Content.ReadAsStringAsync());
                        this.background = null;
                        mapData = null;
                    }
                 
                }

                if (currentGameData.MapSeed != 0 && this.mapData == null)
                {
                    GetMapData();
                }
                pictureBox1.Refresh();
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Graphics graphics = null;
            Graphics backgroundGraphics;
            Bitmap updatedMap;

            if (this.mapData == null)
            {
                return;
            }

            if (this.background == null)
            {

                MapData mapData = this.mapData;

                background = new Bitmap(mapData.mapRows[0].Length, mapData.mapRows.Length, PixelFormat.Format32bppArgb);
                backgroundGraphics = Graphics.FromImage(background);
                backgroundGraphics.SmoothingMode = SmoothingMode.AntiAlias;
                backgroundGraphics.FillRectangle(new SolidBrush(Color.FromArgb(0, 0, 0)), 0, 0, mapData.mapRows[0].Length, mapData.mapRows.Length);

                var doorImgNext = new Bitmap(10, 10, PixelFormat.Format32bppArgb);
                graphics = Graphics.FromImage(doorImgNext);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.FillRectangle(new SolidBrush(Color.FromArgb(237, 107, 0)), 0, 0, 10, 10);

                var doorImgPrev = new Bitmap(10, 10, PixelFormat.Format32bppArgb);
                graphics = Graphics.FromImage(doorImgPrev);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.FillRectangle(new SolidBrush(Color.FromArgb(255, 0, 149)), 0, 0, 10, 10);

                var waypointImg = new Bitmap(10, 10, PixelFormat.Format32bppArgb);
                graphics = Graphics.FromImage(waypointImg);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.FillRectangle(new SolidBrush(Color.FromArgb(16, 140, 235)), 0, 0, 10, 10);


                for (int x = 0; x < mapData.mapRows.Length; x++)
                {
                    for (int y = 0; y < mapData.mapRows[x].Length; y++)
                    {
                        if (mapData.mapRows[x][y] == 1)
                        {
                            background.SetPixel(y, x, Color.FromArgb(0, 0, 0));
                        }
                        else if (mapData.mapRows[x][y] == -1)
                        {
                            background.SetPixel(y, x, Color.FromArgb(79, 40, 0));
                        }
                        else if (mapData.mapRows[x][y] == 0)
                        {
                            background.SetPixel(y, x, Color.FromArgb(255, 255, 255));
                        }
                        else if (mapData.mapRows[x][y] == 16)
                        {
                            background.SetPixel(y, x, Color.FromArgb(168, 56, 50));
                        }
                        else if (mapData.mapRows[x][y] == 7)
                        {
                            background.SetPixel(y, x, Color.FromArgb(36, 42, 150));
                        }
                    }
                }

                int counter = 0;
                int originX = mapData.levelOrigin.x;
                int originY = mapData.levelOrigin.y;

                foreach (KeyValuePair<string, AdjacentLevel> i in mapData.adjacentLevels)
                {
                    if (mapData.adjacentLevels[i.Key].exits.Length == 0)
                    {
                        continue;
                    }

                    int xnew = mapData.adjacentLevels[i.Key].exits[0].x;
                    int ynew = mapData.adjacentLevels[i.Key].exits[0].y;

                    int xcoord = xnew - originX;
                    int ycoord = ynew - originY;
                    if (counter == 0)
                    {
                        backgroundGraphics.DrawImage(doorImgPrev, new Point(xcoord, ycoord));
                    }
                    else
                    {
                        backgroundGraphics.DrawImage(doorImgNext, new Point(xcoord, ycoord));
                    }
                    counter++;

                }

                foreach (KeyValuePair<string, XY[]> mapObject in mapData.objects)
                {
                    if (mapData.objects[mapObject.Key].Length == 1)
                    {
                        backgroundGraphics.DrawImage(waypointImg, new Point(mapData.objects[mapObject.Key][0].x - originX, mapData.objects[mapObject.Key][0].y - originY));
                    }
                }

            }


            updatedMap = (Bitmap)background.Clone();
            var playerLoc = new Bitmap(5, 5, PixelFormat.Format32bppArgb);
            graphics = Graphics.FromImage(playerLoc);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.FillRectangle(new SolidBrush(Color.FromArgb(255, 0, 0)), 0, 0, 5, 5);
            backgroundGraphics = Graphics.FromImage(updatedMap);
            backgroundGraphics.DrawImage(playerLoc, new Point(this.currentGameData.PlayerX - mapData.levelOrigin.x, this.currentGameData.PlayerY - mapData.levelOrigin.y));
   

            // var backgroundCropped = new Bitmap(maxX, maxY, PixelFormat.Format32bppArgb);
            // graphics = Graphics.FromImage(backgroundCropped);
            //graphics.PixelOffsetMode = PixelOffsetMode.Half;
            //graphics.TranslateTransform(100, 100);
            //graphics.RotateTransform(45);
            // graphics.DrawImage(background, new Point(0, 0));

            //g.RotateTransform(45);
            // g.ScaleTransform(0.5F, 0.5F);



            //g.DrawImage(updatedMap, new Point((e.ClipRectangle.Width  - updatedMap.Width) / 2, (e.ClipRectangle.Height - updatedMap.Height) / 2 ));
            g.DrawImage(updatedMap, new Point(0, 0));

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private async void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.background = null;
            GetMapData();
            pictureBox1.Refresh();
        }

        private async void GetMapData()
        {
            if (mapApiSession == null)
            {
                return;
            }

            // Get the currently selected item in the ListBox.

            using (HttpClient client = new HttpClient())
            {
                int index = listBox1.SelectedIndex;

                if (index == -1)
                {
                    return;
                }

                Console.WriteLine(index);
                HttpResponseMessage response = await client.GetAsync("http://localhost:8080/sessions/" + mapApiSession.id + "/areas/" + index);
                this.mapData = JsonConvert.DeserializeObject<MapData>(await response.Content.ReadAsStringAsync());
            }
        }
    }
}
