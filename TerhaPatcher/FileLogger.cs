using System.IO;


namespace TerhaPatcher
{
    public interface ILogger
    {
        void Log(string message, string level = "INFO");
    }
    public class FileLogger : ILogger
    {
        private readonly string logFilePath;

        public FileLogger(string logFilePath)
        {
            this.logFilePath = logFilePath;
        }

        public void Log(string message, string level)
        {
            using (StreamWriter logWriter = File.AppendText(logFilePath))
            {
                logWriter.WriteLine($"[{DateTime.Now}][{level}] {message}");
            }
        }
    }
}
