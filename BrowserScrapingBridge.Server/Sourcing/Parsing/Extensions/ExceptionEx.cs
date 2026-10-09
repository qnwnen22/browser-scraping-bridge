#nullable disable warnings
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions
{
    public static class ExceptionEx
    {
        /// <summary>
        /// 예외처리를 콘솔창에 띄웁니다.
        /// </summary>
        /// <param name="exception"></param>
        public static void Show(this Exception exception)
        {
            string stackTrace = exception.StackTrace;
            Console.WriteLine(stackTrace);

            string message = exception.Message;
            Console.WriteLine(message);

            Debug.WriteLine(stackTrace);
            Debug.WriteLine(message);
        }
        /// <summary>
        /// 빌드된 상태에서 로그를 추적하기위해 만듬.
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="logger"></param>
        public static void ShowLog(this Exception exception, ILogger logger)
        {
            string stackTrace = exception.StackTrace;
            string message = exception.Message;

            logger.LogWarning(stackTrace);
            logger.LogWarning(message);
        }
    }
}
