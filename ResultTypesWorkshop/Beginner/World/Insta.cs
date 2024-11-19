namespace Beginner.World
{
    static class Insta
    {
        public static void Post(Picture picture)
        {
            Console.WriteLine("Cool picture!!!");
            Posts++;
        }

        public static void Post(string text)
        {
            Console.WriteLine(text);
            Posts++;
        }

        public static int Posts { get; private set; }
    }

    record Picture;
}
