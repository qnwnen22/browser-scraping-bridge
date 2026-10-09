#nullable disable warnings
namespace BrowserScrapingBridge.Server.Sourcing.Common
{
    public class CollectExtension
    {
        /// <summary>
        /// 수집 속도를 늦추기 위해 사용
        /// </summary>
        /// <param name="millisecondsTimeout"></param>
        public static void CollectSleep(int? millisecondsTimeout = null)
        {

#if DEBUG || HLC
#else
            int value = millisecondsTimeout == null ? 500 : (int)millisecondsTimeout;
            System.Threading.Tasks.Task.Delay(value);
#endif
        }
    }
}
