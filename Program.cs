namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            ServerManager.Instance.Play();
            Console.ReadKey();
        }
    }
}