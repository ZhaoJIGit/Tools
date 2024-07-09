using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ImageBlur
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string imagePath = "1.png";
            //string outputPath = "2.png";

            // 打开原始图像
            using (Image<Rgba32> originalImage = Image.Load<Rgba32>(imagePath))
            {
                float[] radii = { 0f, 1f, 5f, 10f, 20f,50f,80f,100f };
                int name =10;
                foreach (float radius in radii)
                { 
                    
                    // 应用高斯模糊
                    using (Image<Rgba32> blurredImage = originalImage.Clone(ctx => ctx.GaussianBlur(radius)))
                    {
                        // 混合原始图像和模糊图像
                        using (Image<Rgba32> blendedImage = BlendImages(originalImage, blurredImage, 0.5f))
                        {
                            // 保存结果
                            blendedImage.Save(name+"_b.png");
                        }
                    }
                    name++;
                }
            }
        }

        static Image<Rgba32> BlendImages(Image<Rgba32> original, Image<Rgba32> blurred, float alpha)
        {
            if (original.Width != blurred.Width || original.Height != blurred.Height)
                throw new ArgumentException("Images must be the same size.");

            Image<Rgba32> blended = original.Clone();

            blended.Mutate(ctx => ctx.DrawImage(blurred, new GraphicsOptions
            {
                AlphaCompositionMode = PixelAlphaCompositionMode.SrcOver,
                BlendPercentage = alpha
            }));

            return blended;
        }
    }
}
