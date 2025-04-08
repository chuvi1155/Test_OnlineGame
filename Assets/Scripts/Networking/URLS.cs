namespace Networking
{
    public class URLS
    {
        static string Domain => "https://plinkingbooks.com";

        /// <summary>
        /// POST request config url
        /// </summary>
        public static string ConfigUrl => $"{Domain}/config.php"; // POST
    } 
}