#nullable disable warnings
namespace BrowserScrapingBridge.Server.Sourcing.Parsing.Tool
{
    public class DebugConsole
    {
        private DebugConsole() { }
        /// <summary>
        /// 디버깅 환경에서만 콘솔 log가 작성되도록 전처리된 함수
        /// </summary>
        /// <param name="text"></param>
        public static void Write(string? text)
        {
#if DEBUG || HLC
            text += " ";
            Console.Write(text);
#else

#endif
        }
        /// <summary>
        /// 디버깅 환경에서만 콘솔 log가 작성되도록 전처리된 함수
        /// </summary>
        /// <param name="text"></param>
        public static void WriteLine(string? text = "")
        {
#if DEBUG || HLC
            //if (string.IsNullOrWhiteSpace(text)) return;
            Console.WriteLine(text);
#else

#endif
        }
    }
}
