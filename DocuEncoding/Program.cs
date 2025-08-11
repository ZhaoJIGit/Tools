using Microsoft.International.Converters.TraditionalChineseToSimplifiedConverter;
using System.Text;

namespace DocuEncoding
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // 注册 CodePagesEncodingProvider 来支持更多编码，包括 GB2312
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var dirc = Path.Combine(Directory.GetCurrentDirectory(),"files");
            string[] txtFiles = Directory.GetFiles(dirc, "*.txt");

            foreach (var file in txtFiles)
            {
                FileInfo fileInfo = new FileInfo(file);

                // 输入文件路径和输出文件路径
                string inputFilePath = file;
                string outputFilePath = Path.Combine("D:/6、工作文件/books", fileInfo.Name.Replace(".txt","_new.txt")); ;
                // 指定源文件的编码为 GB2312，并将其转换为目标编码 UTF-8
                ConvertFileEncoding(inputFilePath, outputFilePath, Encoding.GetEncoding("GB2312"), Encoding.UTF8, ChineseConversionDirection.TraditionalToSimplified);

                Console.WriteLine("文件编码转换完成！");

            }
            Console.WriteLine("已全部转码！");


        }

        static void ConvertFileEncoding(string inputFilePath, string outputFilePath, Encoding sourceEncoding, Encoding targetEncoding, ChineseConversionDirection direction)
        {
            // 检查输入文件是否存在
            if (!File.Exists(inputFilePath))
            {
                Console.WriteLine("输入文件不存在。");
                return;
            }

            try
            {
                // 使用 StreamReader 读取文件内容并指定源编码
                using (StreamReader reader = new StreamReader(inputFilePath, sourceEncoding))
                // 使用 StreamWriter 写入文件内容并指定目标编码
                using (StreamWriter writer = new StreamWriter(outputFilePath, false, targetEncoding))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // 使用 ChineseConverter 进行转换
                        string convertedContent = ChineseConverter.Convert(line, direction);

                        writer.WriteLine(convertedContent);
                    }
                }

                Console.WriteLine($"文件已成功从 {sourceEncoding.EncodingName} 编码转换为 {targetEncoding.EncodingName} 编码。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"文件转换失败: {ex.Message}");
            }
        }
    }
}
