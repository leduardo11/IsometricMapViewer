using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace IsometricMapViewer.Rendering
{
    public class TextureLoader(GraphicsDevice graphicsDevice)
    {
        private readonly GraphicsDevice _graphicsDevice = graphicsDevice;
        private readonly SpriteBatch _spriteBatch = new SpriteBatch(graphicsDevice);

        private static readonly BlendState BlendColorBlendState = new()
        {
            ColorDestinationBlend = Blend.Zero,
            ColorWriteChannels = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue,
            AlphaDestinationBlend = Blend.Zero,
            AlphaSourceBlend = Blend.SourceAlpha,
            ColorSourceBlend = Blend.SourceAlpha
        };

        private static readonly BlendState BlendAlphaBlendState = new BlendState
        {
            ColorWriteChannels = ColorWriteChannels.Alpha,
            AlphaDestinationBlend = Blend.Zero,
            ColorDestinationBlend = Blend.Zero,
            AlphaSourceBlend = Blend.One,
            ColorSourceBlend = Blend.One
        };

        public Texture2D FromStream(Stream stream, bool preMultiplyAlpha = true)
        {
            Texture2D texture = Texture2D.FromStream(_graphicsDevice, stream);

            if (preMultiplyAlpha)
            {
                using RenderTarget2D renderTarget = new(
                    _graphicsDevice,
                    texture.Width,
                    texture.Height,
                    false,
                    SurfaceFormat.Color,
                    DepthFormat.None);

                Viewport viewportBackup = _graphicsDevice.Viewport;

                try
                {
                    _graphicsDevice.SetRenderTarget(renderTarget);
                    _graphicsDevice.Clear(Color.Black);
                    _spriteBatch.Begin(SpriteSortMode.Deferred, BlendColorBlendState);
                    _spriteBatch.Draw(texture, texture.Bounds, Color.White);
                    _spriteBatch.End();
                    _spriteBatch.Begin(SpriteSortMode.Deferred, BlendAlphaBlendState);
                    _spriteBatch.Draw(texture, texture.Bounds, Color.White);
                    _spriteBatch.End();
                    Color[] data = new Color[texture.Width * texture.Height];
                    renderTarget.GetData(data);
                    _graphicsDevice.SetRenderTarget(null);
                    _graphicsDevice.Textures[0] = null;
                    texture.SetData(data);
                }
                finally
                {
                    _graphicsDevice.Viewport = viewportBackup;
                }
            }
            return texture;
        }
    }
}