namespace AuthService.Helpers
{
    public static class InitialGenerator
    {
        public static string Generate(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return "??";

            return new string(username.ToUpper().Take(2).ToArray());
        }
    }
}
