namespace Wale.Configs
{
    public static class Manager
    {
        public static General GL = new General();
        public static string WorkingPath => GL.WorkingPath;

        public static void Initialize()
        {
            GL.PathInit();
            M.InitF(WorkingPath, "WaleLog", 14);
            GL.Init();
        }
    }
}
