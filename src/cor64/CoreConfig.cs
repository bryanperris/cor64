namespace cor64 {
    public class CoreConfig {
        public static CoreConfig Current { get; } = new CoreConfig();

        public bool WorkbenchMode { get; set; } = false;
    }
}