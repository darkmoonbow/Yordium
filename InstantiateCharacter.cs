using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows.Forms;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Physics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using LiteNetLib;
using LiteNetLib.Utils;
using DotNetEnv;
using Stride.UI.Controls;
using System.Drawing;
using System.Drawing.Imaging;
using Valve.VR;
using Stride.Animations;

namespace Yordium1
{
    public class Avatar
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public int? HeadColor { get; set; }
        public int? TorsoColor { get; set; }
        public int? LegsColor { get; set; }
        public int? ArmsColor { get; set; }
        public int? TshirtSlot { get; set; }
        public int? FaceSlot { get; set; }
    }

    public class ServerInfo
    {
        public string ip { get; set; }
        public int port { get; set; }
        public int players { get; set; }
    }

    public class GameClient : INetEventListener
    {
        public static GameClient Instance { get; private set; }

        public GameClient()
        {
            Instance = this;
        }

        private NetManager client;
        private NetPeer serverPeer;

        public Avatar PlayerInfo { get; set; }
        public UIPage LeaderboardPanel {get; set;}
        
        public NetPeer ServerPeer => serverPeer;
        private readonly Logger log = GlobalLogger.GetLogger("Network");

        public Dictionary<int, Entity> OtherPlayers = new Dictionary<int, Entity>();
        public Model CharacterModel;
        public Scene CurrentScene { get; set; }

        private void UpdateLeaderboardUI(List<string> names)
        {
            foreach (var child in LeaderboardPanel.RootElement.VisualChildren)
            {
                if (child is TextBlock textblock)
                {
                    textblock.Text = string.Join("\n", names);

                    return;
                }
            }
            throw new FileNotFoundException("Leaderboard file not found!");
        }

        
        
        public void Connect(string ip, int port)
        {
            client = new NetManager(this) { AutoRecycle = true };
            client.Start();
            client.Connect(ip, port, "yordium_key");
            log.Info($"Connecting to {ip}:{port}...");
        }

        public void Poll() => client?.PollEvents();

        public void OnPeerConnected(NetPeer peer)
        {
            serverPeer = peer;
            log.Info("Connected to server!");
            SendAuth();
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info)
        {
            log.Info($"Disconnected from server: {info.Reason}");
        }

        public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
        {
            log.Warning($"Network error: {socketError} at {endPoint}");
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            string msg = reader.GetString();
            log.Info($"Received: {msg}");

            var parts = msg.Split('|');
            if (parts.Length == 0) return;

            switch (parts[0])
            {
                case "SPAWN":
                    HandleSpawn(parts);
                    break;
                case "POS":
                    HandlePosition(parts);
                    break;
                case "LEADERBOARD":
                    int count = reader.GetInt();
                    List<string> leaderboard = new List<string>();
                    for (int i = 0; i < count; i++)
                    {
                        int id = reader.GetInt();
                        string username = reader.GetString();
                        leaderboard.Add(username);
                    }
                    UpdateLeaderboardUI(leaderboard);
                    break;
            }
        }

        private void HandleSpawn(string[] parts)
        {
            if (parts.Length != 9) return;

            int id = int.Parse(parts[1]);
            if (OtherPlayers.ContainsKey(id)) return;

            string username = parts[2];
            float x = float.Parse(parts[3]);
            float y = float.Parse(parts[4]);
            float z = float.Parse(parts[5]);
            float rotX = float.Parse(parts[6]);
            float rotY = float.Parse(parts[7]);
            float rotZ = float.Parse(parts[8]);

            var newPlayer = new Entity($"Player_{id}");
            var modelComponent = new ModelComponent { Model = CharacterModel };
            newPlayer.Add(modelComponent);
            newPlayer.Transform.Position = new Vector3(x, y, z);
            newPlayer.Transform.RotationEulerXYZ = new Vector3(rotX, rotY, rotZ);

            CurrentScene.Entities.Add(newPlayer);
            OtherPlayers[id] = newPlayer;

            log.Info($"Spawned player {username} ({id})");
        }

        private void HandlePosition(string[] parts)
        {
            if (parts.Length != 8) return;

            int id = int.Parse(parts[1]);
            if (!OtherPlayers.TryGetValue(id, out var entity)) return;

            float x = float.Parse(parts[2]);
            float y = float.Parse(parts[3]);
            float z = float.Parse(parts[4]);
            float rotX = float.Parse(parts[5]);
            float rotY = float.Parse(parts[6]);
            float rotZ = float.Parse(parts[7]);

            entity.Transform.Position = new Vector3(x, y, z);
            entity.Transform.RotationEulerXYZ = new Vector3(rotX, rotY, rotZ);
        }

        public void OnConnectionRequest(ConnectionRequest request) => request.AcceptIfKey("yordium_key");
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
        public void OnNetworkReceiveUnconnected(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType type) { }

        private void SendAuth()
        {
            if (serverPeer == null) return;

            var writer = new NetDataWriter();
            writer.Put("AUTH");
            writer.Put(PlayerInfo.Username ?? "Player");
            serverPeer.Send(writer, DeliveryMethod.ReliableOrdered);
            log.Info("Sent AUTH packet");
        }

        public void SendPosition(Vector3 pos, Vector3 rot)
        {
            if (serverPeer == null) return;

            var writer = new NetDataWriter();
            writer.Put("POS");
            writer.Put(PlayerInfo.Id);
            writer.Put(pos.X);
            writer.Put(pos.Y);
            writer.Put(pos.Z);
            writer.Put(rot.X);
            writer.Put(rot.Y);
            writer.Put(rot.Z);
            serverPeer.Send(writer, DeliveryMethod.Unreliable);
        }
    }

    public class InstantiateCharacter : AsyncScript
    {
        public Model CharacterModel { get; set; }
        public Entity MainCamera { get; set; }
        public Scene scene { get; set; }
        public UIPage LeaderboardPanel { get; set; }

        private GameClient gameClient;
        private Logger log = GlobalLogger.GetLogger("Game");

        private Color4[] palette =
            {
                new Color4(1f, 0.8f, 0.6f, 1f),
                new Color4(1f, 0.6f, 0.4f, 1f),
                new Color4(0.9f, 0.7f, 0.5f, 1f),
                new Color4(0.7f, 0.5f, 0.3f, 1f),
                new Color4(0.5f, 0.3f, 0.2f, 1f),
                new Color4(0.3f, 0.2f, 0.1f, 1f),
                new Color4(0.9f, 0.8f, 0.7f, 1f),
                new Color4(0.2f, 0.2f, 0.2f, 1f),

                new Color4(1f, 0f, 0f, 1f),       // Red
                new Color4(1f, 0.5f, 0f, 1f),     // Orange
                new Color4(1f, 1f, 0f, 1f),       // Yellow
                new Color4(0f, 1f, 0f, 1f),       // Green
                new Color4(0f, 0f, 1f, 1f),       // Blue
                new Color4(0.29f, 0f, 0.51f, 1f), // Indigo
                new Color4(0.56f, 0f, 1f, 1f),    // Violet

                new Color4(1f, 1f, 1f, 1f),       // White
                new Color4(0.7f, 0.7f, 0.7f, 1f), // Light gray
                new Color4(0.4f, 0.4f, 0.4f, 1f), // Gray
                new Color4(0f, 0f, 0f, 1f),       // Black
            };

        public override async Task Execute()
        {
            Env.Load(Path.Combine(AppContext.BaseDirectory, ".env"));
            log.Info($"Loaded .env from: {System.IO.Path.Combine(AppContext.BaseDirectory, ".env")}");
            log.Info($"SERVER_IP: {Environment.GetEnvironmentVariable("SERVER_IP")}");

            string serverIp = Env.GetString("SERVER_IP", "localhost");

            int serverPort = Env.GetInt("SERVER_PORT", 8000);
            int gameServerPort = Env.GetInt("GAME_SERVER_PORT", 9050);

            string token = ParseTokenFromArgs();
            log.Info(token);

            if (string.IsNullOrEmpty(token))
                throw new InvalidOperationException("No token provided! Cannot authenticate the player.");

            using var httpClient = new HttpClient();

            var userResponse = await httpClient.GetAsync($"http://{serverIp}:{serverPort}/api/game-auth/{token}");
            userResponse.EnsureSuccessStatusCode();
            var userJson = await userResponse.Content.ReadAsStringAsync();
            var avatar = JsonConvert.DeserializeObject<Avatar>(userJson);
            log.Info($"Logged in as {avatar.Username} (ID: {avatar.Id})");

            var serverJson = await httpClient.GetStringAsync($"http://{serverIp}:{serverPort}/get-server");
            var serverInfo = JsonConvert.DeserializeObject<ServerInfo>(serverJson);

            

            gameClient = new GameClient
            {
                PlayerInfo = avatar,
                CharacterModel = CharacterModel,
                CurrentScene = scene,
                LeaderboardPanel = LeaderboardPanel
            };

            _ = Task.Run(async () =>
            {
                gameClient.Connect(serverInfo.ip, serverInfo.port);

                while (gameClient.ServerPeer == null)
                {
                    gameClient.Poll();
                    await Task.Delay(15);
                }

                log.Info("Connected to server.");

                while (true)
                {
                    gameClient.Poll();

                    if (scene != null)
                    {
                        var playerEntity = scene.Entities.FirstOrDefault(e => e.Name == "Player");
                        if (playerEntity != null)
                            gameClient.SendPosition(playerEntity.Transform.Position, playerEntity.Transform.RotationEulerXYZ);
                    }

                    await Task.Delay(50);
                }
            });


            var playerEntitySpawn = new Entity("Player");
            var modelComponent = new ModelComponent { Model = CharacterModel };
            playerEntitySpawn.Add(modelComponent);
            this.SceneSystem.SceneInstance.RootScene.Entities.Add(playerEntitySpawn);

            var characterComponent = new CharacterComponent { JumpSpeed = 5, FallSpeed = 20 };
            var playerMovement = new PlayerMovement { CameraEntity = MainCamera, UIPanel = LeaderboardPanel };
            var collider = new CapsuleColliderShapeDesc { Is2D = false, Radius = 0.5f, Length = 2f, Orientation = ShapeOrientation.UpY };
            characterComponent.ColliderShapes.Add(collider);

            var animationComponent = Entity.Get<AnimationComponent>();
            Entity.Remove(animationComponent);

            log.Info((animationComponent == null).ToString());

            playerEntitySpawn.Transform.Position = new Vector3(0, 1, 0);
            playerEntitySpawn.Add(characterComponent);
            playerEntitySpawn.Add(animationComponent);
            playerEntitySpawn.Add(playerMovement);
            


            var orbitCam = new OrbitCam { Target = playerEntitySpawn };
            MainCamera.Add(orbitCam);

            await ApplyAvatarAppearance(playerEntitySpawn, avatar, httpClient, serverIp, serverPort);
        }

        private string ParseTokenFromArgs()
        {
            string[] args = Environment.GetCommandLineArgs();
            string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "yordium_log.txt");
            File.AppendAllLines(logPath, new[] { DateTime.Now.ToString(), "Args: " + string.Join(", ", args) });

            string urlArg = args.FirstOrDefault(a => a.Trim('"').StartsWith("yordium://"))?.Trim('"');
            if (urlArg == null)
            {
                urlArg = "yordium://launch?token=jzUMWbBEwQydsJx9uRY00MfBkE97fPSW4R2cscIB&place=5";
                //throw new InvalidOperationException("No protocol URL provided.");
            }

            var uri = new Uri(urlArg);
            var query = uri.Query.TrimStart('?').Split('&').Select(q => q.Split('=')).ToDictionary(p => p[0], p => p[1]);

            if (!query.ContainsKey("token")) throw new InvalidOperationException("No token provided in the URL.");
            return query["token"];
        }

        async Task<Texture> BlendTexture(HttpClient client, string url, Color4 bgColor, GraphicsDevice GraphicsDevice, bool rotate)
        {
            var skinColor = System.Drawing.Color.FromArgb(
                (int)(bgColor.A * 255),
                (int)(bgColor.R * 255),
                (int)(bgColor.G * 255),
                (int)(bgColor.B * 255)
            );
            var bytes = await client.GetByteArrayAsync(url);


            using (var ms = new MemoryStream(bytes))
            using(var bmp = new Bitmap(ms))
            {
                Bitmap blended = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                using (var g = Graphics.FromImage(blended))
                {
                    g.Clear(skinColor);
                    g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);
                }

                if (rotate)
                {
                    blended.RotateFlip(RotateFlipType.Rotate270FlipNone);
                }

                using (var tempStream = new MemoryStream())
                {
                    blended.Save(tempStream, ImageFormat.Png);
                    tempStream.Position = 0;
                    return Texture.Load(GraphicsDevice, tempStream);
                }
            }
        }

        private async Task ApplyAvatarAppearance(Entity playerEntity, Avatar appearance, HttpClient client, string serverIp, int serverPort)
        {
            

            Material CreateColor(int? idx)
            {
                if (idx == null || idx <= 0 || idx > palette.Length) idx = 1;
                var color = new ComputeColor(palette[(int)idx - 1]);
                return Material.New(GraphicsDevice, new MaterialDescriptor
                {
                    Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature { DiffuseMap = color },
                    DiffuseModel = new MaterialDiffuseLambertModelFeature()
                }
                });
            }

            async Task<Texture> LoadTexture(string url)
            {
                var bytes = await client.GetByteArrayAsync(url);
                using var stream = new MemoryStream(bytes);
                return Texture.Load(GraphicsDevice, stream);
            }

            
            


            CharacterModel.Materials[0].Material = CreateColor(appearance.HeadColor);
            CharacterModel.Materials[1].Material = CreateColor(appearance.HeadColor);
            for (int i = 2; i <= 7; i++) CharacterModel.Materials[i].Material = CreateColor(appearance.ArmsColor);
            for (int i = 8; i <= 13; i++) CharacterModel.Materials[i].Material = CreateColor(appearance.LegsColor);
            for (int i = 14; i <= 19; i++) CharacterModel.Materials[i].Material = CreateColor(appearance.ArmsColor);
            for (int i = 20; i <= 25; i++) CharacterModel.Materials[i].Material = CreateColor(appearance.LegsColor);
            for (int i = 26; i <= 31; i++) CharacterModel.Materials[i].Material = CreateColor(appearance.TorsoColor);

            Texture shirtTex = null;
            Texture faceTex = null;

            string faceUrl =  $"http://{serverIp}:{serverPort}/assets/faces/{appearance.FaceSlot}.png";
            faceTex = await BlendTexture(client, faceUrl, palette[(int)appearance.HeadColor - 1], GraphicsDevice, false);

            string shirtUrl = $"http://{serverIp}:{serverPort}/assets/clothing/{appearance.TshirtSlot}.png";
            shirtTex = await BlendTexture(client, shirtUrl, palette[(int)appearance.TorsoColor - 1], GraphicsDevice, true);

            var computeColorShirt = new ComputeTextureColor(shirtTex);
            computeColorShirt.Scale = new Vector2(4f, 4f);
            computeColorShirt.Offset = new Vector2(0.5f, 0f);


            var computeColorFace = new ComputeTextureColor(faceTex);


            CharacterModel.Materials[26].Material = Material.New(GraphicsDevice, new MaterialDescriptor{Attributes ={Diffuse = new MaterialDiffuseMapFeature{DiffuseMap = computeColorShirt},DiffuseModel = new MaterialDiffuseLambertModelFeature(),}});
            CharacterModel.Materials[1].Material = Material.New(GraphicsDevice, new MaterialDescriptor { Attributes = { Diffuse = new MaterialDiffuseMapFeature { DiffuseMap = computeColorFace }, DiffuseModel = new MaterialDiffuseLambertModelFeature(), } });
        }


    }
}
