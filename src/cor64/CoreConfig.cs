namespace cor64 {
    public class CoreConfig {
        public static CoreConfig Current { get; } = new CoreConfig();

        public bool ByteSwap { get; set; } = true;
        public bool IsNativeBigEndian { get; set; } = false;
    }
}