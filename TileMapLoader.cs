using System;
using System.IO;
using Newtonsoft.Json.Linq;
using SDL2;

namespace Demo
{
    class TiledMapLoader
    {
        static int cameraX = 0;
        static int cameraY = 0;
        static int screenWidth = 800;
        static int screenHeight = 600;
        static int tileWidth;
        static int tileHeight;
        static int mapWidth;
        static int mapHeight;

        // Character animation variables
        static IntPtr characterTexture;
        static int characterTileX = 5; // Initial tile position
        static int characterTileY = 5; // Initial tile position
        static int characterX;
        static int characterY;
        static int characterWidth = 64;
        static int characterHeight = 64;
        static int characterFrame = 0;
        static int totalFrames = 8; // Number of frames in the sprite sheet
        static int frameDelay = 100; // Time in milliseconds to show each frame
        static uint lastFrameTime = 0;

        static void Main(string[] args)
        {
            // Initialize SDL2
            if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
            {
                Console.WriteLine($"SDL could not initialize! SDL_Error: {SDL.SDL_GetError()}");
                return;
            }

            IntPtr window = SDL.SDL_CreateWindow("Tiled Map", SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED, screenWidth, screenHeight, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);
            if (window == IntPtr.Zero)
            {
                Console.WriteLine($"Window could not be created! SDL_Error: {SDL.SDL_GetError()}");
                return;
            }

            IntPtr renderer = SDL.SDL_CreateRenderer(window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
            if (renderer == IntPtr.Zero)
            {
                Console.WriteLine($"Renderer could not be created! SDL_Error: {SDL.SDL_GetError()}");
                return;
            }

            // Load the JSON file
            string jsonPath = "map.json";
            string jsonString = File.ReadAllText(jsonPath);

            // Parse the JSON file
            JObject mapData = JObject.Parse(jsonString);

            // Extract map properties
            mapWidth = (int)mapData["width"];
            mapHeight = (int)mapData["height"];
            tileWidth = (int)mapData["tilewidth"];
            tileHeight = (int)mapData["tileheight"];

            Console.WriteLine($"Map Size: {mapWidth}x{mapHeight} tiles");
            Console.WriteLine($"Tile Size: {tileWidth}x{tileHeight} pixels");

            // Load the tileset
            string tilesetImagePath = "world_tileset.png";
            IntPtr tilesetTexture = LoadTileset(renderer, tilesetImagePath);

            // Load the character sprite sheet
            string characterImagePath = "RUN.png";
            characterTexture = LoadTileset(renderer, characterImagePath);

            // Set character initial position based on tile coordinates
            characterX = characterTileX * tileWidth;
            characterY = characterTileY * tileHeight;

            // Process layers
            JArray layers = (JArray)mapData["layers"];
            int[] tileGIDs = layers[0]["data"].ToObject<int[]>();

            // Main loop
            bool quit = false;
            SDL.SDL_Event e;
            while (!quit)
            {
                while (SDL.SDL_PollEvent(out e) != 0)
                {
                    if (e.type == SDL.SDL_EventType.SDL_QUIT)
                    {
                        quit = true;
                    }
                    else if (e.type == SDL.SDL_EventType.SDL_KEYDOWN)
                    {
                        switch (e.key.keysym.sym)
                        {
                            case SDL.SDL_Keycode.SDLK_LEFT:
                                cameraX -= 10;
                                if (cameraX < 0) cameraX = 0;
                                break;
                            case SDL.SDL_Keycode.SDLK_RIGHT:
                                cameraX += 10;
                                if (cameraX > mapWidth * tileWidth - screenWidth)
                                    cameraX = mapWidth * tileWidth - screenWidth;
                                break;
                            case SDL.SDL_Keycode.SDLK_UP:
                                cameraY -= 10;
                                if (cameraY < 0) cameraY = 0;
                                break;
                            case SDL.SDL_Keycode.SDLK_DOWN:
                                cameraY += 10;
                                if (cameraY > mapHeight * tileHeight - screenHeight)
                                    cameraY = mapHeight * tileHeight - screenHeight;
                                break;
                        }
                    }
                }

                // Clear the screen
                SDL.SDL_RenderClear(renderer);

                // Render the visible part of the map
                for (int y = 0; y < mapHeight; y++)
                {
                    for (int x = 0; x < mapWidth; x++)
                    {
                        int tileIndex = y * mapWidth + x;
                        int tileGID = tileGIDs[tileIndex];
                        if (tileGID != 0)
                        {
                            int renderX = x * tileWidth - cameraX;
                            int renderY = y * tileHeight - cameraY;

                            // Only render tiles that are within the camera's view
                            if (renderX + tileWidth > 0 && renderX < screenWidth && renderY + tileHeight > 0 && renderY < screenHeight)
                            {
                                RenderTile(renderer, tilesetTexture, tileGID, renderX, renderY, tileWidth, tileHeight);
                            }
                        }
                    }
                }

                // Render the character
                RenderCharacter(renderer);

                // Update the screen
                SDL.SDL_RenderPresent(renderer);
                SDL.SDL_Delay(16); // ~60 FPS
            }

            // Clean up and quit SDL
            SDL.SDL_DestroyTexture(tilesetTexture);
            SDL.SDL_DestroyTexture(characterTexture);
            SDL.SDL_DestroyRenderer(renderer);
            SDL.SDL_DestroyWindow(window);
            SDL.SDL_Quit();
        }

        static IntPtr LoadTileset(IntPtr renderer, string path)
        {
            IntPtr surface = SDL_image.IMG_Load(path);
            if (surface == IntPtr.Zero)
            {
                Console.WriteLine($"Failed to load image: {path}. SDL_image Error: {SDL.SDL_GetError()}");
                return IntPtr.Zero;
            }

            IntPtr texture = SDL.SDL_CreateTextureFromSurface(renderer, surface);
            SDL.SDL_FreeSurface(surface);
            if (texture == IntPtr.Zero)
            {
                Console.WriteLine($"Failed to create texture from surface. SDL Error: {SDL.SDL_GetError()}");
            }

            return texture;
        }

        static void RenderTile(IntPtr renderer, IntPtr tilesetTexture, int tileGID, int x, int y, int tileWidth, int tileHeight)
        {
            // Calculate the tile's position in the tileset
            int tilesetColumns = 256 / tileWidth; // Assuming tileset image width is 256 pixels
            int tilesetX = (tileGID - 1) % tilesetColumns * tileWidth;
            int tilesetY = (tileGID - 1) / tilesetColumns * tileHeight;

            SDL.SDL_Rect srcRect = new SDL.SDL_Rect { x = tilesetX, y = tilesetY, w = tileWidth, h = tileHeight };
            SDL.SDL_Rect destRect = new SDL.SDL_Rect { x = x, y = y, w = tileWidth, h = tileHeight };

            SDL.SDL_RenderCopy(renderer, tilesetTexture, ref srcRect, ref destRect);
        }

        static void RenderCharacter(IntPtr renderer)
        {
            uint currentTime = SDL.SDL_GetTicks();
            if (currentTime > lastFrameTime + frameDelay)
            {
                characterFrame = (characterFrame + 1) % totalFrames;
                lastFrameTime = currentTime;
            }

            int frameX = characterFrame * characterWidth;

            SDL.SDL_Rect srcRect = new SDL.SDL_Rect { x = frameX, y = 0, w = characterWidth, h = characterHeight };
            SDL.SDL_Rect destRect = new SDL.SDL_Rect { x = characterX - cameraX, y = characterY - cameraY, w = characterWidth, h = characterHeight };

            SDL.SDL_RenderCopy(renderer, characterTexture, ref srcRect, ref destRect);
        }
    }
}
