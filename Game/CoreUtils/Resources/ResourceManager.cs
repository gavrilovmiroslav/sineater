using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace SINEATER.Game.CoreUtils.Resources
{
    public class ResourceManager
    {
        public EventHandler? RequestReload = delegate { };

        public void InvokeReload(ContentManager? manager) => RequestReload?.Invoke(this, new EventArgs());

        public ResourceManager() 
        {
            // if you need dynamic creation of resource, do it here

            for (int i = 0; i < 24; i++)
            {
                Room[i] = new("daynight/" + i.ToString().PadLeft(2, '0'));
            }
        }
        public void Load(ContentManager Content)
        {
            foreach (var t in Inputs) t.Load(Content);
            foreach (var t in Room) t.Load(Content);

            Pixel.Load(Content);
            AllSprites.Load(Content);
            AllSpriteOutlines.Load(Content);
            Logo.Load(Content);
            WorldMap.Load(Content);
            Semi.Load(Content);
            Frames.Load(Content);
            MRMO.Load(Content);
            Mapmotext.Load(Content);
            IBM.Load(Content);
            InputText.Load(Content);
            LargeNumbers.Load(Content);
            Portraits.Load(Content);
            SpriteShadow.Load(Content);
            Pins.Load(Content);
            Monitor.Load(Content);

            Font.Load(Content);
            FontBold.Load(Content);
            FontMono.Load(Content);
        }

        /* Textures */

        public ResourceHandle<Texture2D>[] Room = new ResourceHandle<Texture2D>[24];

        public ResourceHandle<Texture2D> Pixel = new("pixel");
        public ResourceHandle<Texture2D> AllSprites = new("sprites/all-sprites");
        public ResourceHandle<Texture2D> AllSpriteOutlines = new("sprites/all-sprite-outlines");
        public ResourceHandle<Texture2D> Logo = new("sineater-logo");
        public ResourceHandle<Texture2D> WorldMap = new("Level_0__Tiles");
        public ResourceHandle<Texture2D> Semi = new("semi");
        public ResourceHandle<Texture2D> Frames = new("Frames32px");
        public ResourceHandle<Texture2D> MRMO = new("MRMOTEXT");
        public ResourceHandle<Texture2D> Mapmotext = new("mapmotext");
        public ResourceHandle<Texture2D> IBM = new("Codepage");
        public ResourceHandle<Texture2D> InputText = new("Codepage");
        public ResourceHandle<Texture2D> LargeNumbers = new("largenumbers");
        public ResourceHandle<Texture2D> Portraits = new("swordnsorcery_portraits");
        public ResourceHandle<Texture2D> SpriteShadow = new("sprite_shadow");
        public ResourceHandle<Texture2D> Pins = new("pins");
        public ResourceHandle<Texture2D> Monitor = new("fingerprints");
        public ResourceHandle<Texture2D>[] Inputs = [
                new("inputs/KEYBOARD/KEYBOARD"), // KB
                new("inputs/XBOX/XBOX"), ]; // GP

        /* Fonts */

        public ResourceHandle<SpriteFont> Font = new("eldring");
        public ResourceHandle<SpriteFont> FontMono = new("monogram");
        public ResourceHandle<SpriteFont> FontBold = new("eldring-bold");
    }
}
